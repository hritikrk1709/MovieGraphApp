import { useEffect, useMemo, useRef, useState } from "react";

const GROUP_COLORS = {
  Movie: "#e8b84b",
  Person: "#4fd1c5",
  Genre: "#8a6fce",
};

const DEFAULT_VIEW = "browse";

async function api(path) {
  const res = await fetch(`/api${path}`);
  if (!res.ok) {
    let payload = {};
    try {
      payload = await res.json();
    } catch {
      // ignore json parse failures
    }
    throw new Error(payload.error || `Request failed (${res.status})`);
  }
  return res.json();
}

function debounce(fn, delay) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), delay);
  };
}

function loadVisNetwork() {
  return new Promise((resolve) => {
    if (window.vis) {
      resolve(window.vis);
      return;
    }

    const script = document.createElement("script");
    script.src =
      "https://unpkg.com/vis-network@10.1.0/standalone/umd/vis-network.min.js";
    script.onload = () => resolve(window.vis);
    document.head.appendChild(script);
  });
}

function renderGraph(container, graph, focusId = null) {
  if (!container) return null;

  const vis = window.vis;
  if (!vis) return null;

  const nodes = new vis.DataSet(
    (graph?.nodes || []).map((n) => ({
      id: n.id,
      label: n.label,
      color: {
        background: GROUP_COLORS[n.group] || "#666",
        border: n.id === focusId ? "#fff" : GROUP_COLORS[n.group] || "#666",
        highlight: {
          background: GROUP_COLORS[n.group] || "#666",
          border: "#fff",
        },
      },
    })),
  );

  const edges = new vis.DataSet(
    (graph?.edges || []).map((e) => ({
      id: `${e.from}-${e.to}-${e.type}`,
      from: e.from,
      to: e.to,
      label: e.type,
      arrows: "to",
      color: { color: "#8c95a8", highlight: "#e8b84b" },
      font: { color: "#b7c0d5", size: 10 },
    })),
  );

  const network = new vis.Network(
    container,
    { nodes, edges },
    {
      interaction: { hover: true },
      physics: { enabled: true, stabilization: { iterations: 200 } },
      edges: { smooth: { type: "dynamic" } },
      nodes: {
        font: { color: "#f1efea", face: "Inter", size: 12 },
        borderWidth: 1,
        shadow: true,
      },
    },
  );

  network.fit({ animation: true });
  return network;
}

export default function App() {
  const [activeView, setActiveView] = useState(DEFAULT_VIEW);
  const [error, setError] = useState("");
  const [movies, setMovies] = useState([]);
  const [movieSearch, setMovieSearch] = useState("");
  const [selectedMovieId, setSelectedMovieId] = useState(null);
  const [movieDetail, setMovieDetail] = useState(null);
  const [movieGraph, setMovieGraph] = useState(null);
  const [movieDetailLoading, setMovieDetailLoading] = useState(false);
  const [movieGridLoading, setMovieGridLoading] = useState(true);
  const [users, setUsers] = useState([]);
  const [userId, setUserId] = useState("");
  const [recs, setRecs] = useState([]);
  const [recsLoading, setRecsLoading] = useState(false);
  const [actors, setActors] = useState([]);
  const [fromActor, setFromActor] = useState("");
  const [toActor, setToActor] = useState("");
  const [pathResult, setPathResult] = useState(null);
  const [pathGraph, setPathGraph] = useState(null);
  const [pathLoading, setPathLoading] = useState(false);
  const movieGraphRef = useRef(null);
  const pathGraphRef = useRef(null);

  const movieGraphNetwork = useRef(null);
  const pathGraphNetwork = useRef(null);

  const selectedMovie = useMemo(() => movieDetail, [movieDetail]);

  useEffect(() => {
    loadMovies();
    loadUsers();
    loadActors();
  }, []);

  useEffect(() => {
    if (!movieGraphRef.current || !movieGraph) return;
    if (movieGraphNetwork.current) {
      movieGraphNetwork.current.destroy();
    }
    movieGraphNetwork.current = renderGraph(
      movieGraphRef.current,
      movieGraph,
      selectedMovieId,
    );
  }, [movieGraph, selectedMovieId]);

  useEffect(() => {
    if (!pathGraphRef.current || !pathGraph) return;
    if (pathGraphNetwork.current) {
      pathGraphNetwork.current.destroy();
    }
    pathGraphNetwork.current = renderGraph(
      pathGraphRef.current,
      pathGraph,
      null,
    );
  }, [pathGraph]);

  async function loadMovies(search = "") {
    setMovieGridLoading(true);
    try {
      const data = await api(
        `/movies${search ? `?search=${encodeURIComponent(search)}` : ""}`,
      );
      setMovies(data);
      setError("");
    } catch (err) {
      setMovies([]);
      setError(err.message);
    } finally {
      setMovieGridLoading(false);
    }
  }

  const debouncedLoadMovies = useMemo(
    () => debounce((value) => loadMovies(value), 300),
    [],
  );

  async function loadUsers() {
    try {
      const data = await api("/users");
      setUsers(data);
      if (data.length) {
        setUserId(data[0].id);
      }
      setError("");
    } catch (err) {
      setError(err.message);
    }
  }

  async function loadActors() {
    try {
      const data = await api("/actors");
      setActors(data);

      const defaultFrom = data.find((a) => a.id === "p_ranveer") ?? data[0];
      const defaultTo =
        data.find((a) => a.id === "p_ranbir") ??
        data.find((a) => a.id !== defaultFrom?.id) ??
        defaultFrom;

      if (!fromActor && defaultFrom) setFromActor(defaultFrom.id);
      if ((!toActor || toActor === fromActor) && defaultTo)
        setToActor(defaultTo.id);

      setError("");
    } catch (err) {
      setError(err.message);
    }
  }

  async function selectMovie(id) {
    setSelectedMovieId(id);
    setMovieGraph(null);
    setMovieDetailLoading(true);

    try {
      const [movie, graph] = await Promise.all([
        api(`/movies/${id}`),
        api(`/movies/${id}/graph`),
      ]);
      setMovieDetail(movie);
      setMovieGraph(graph);
      setError("");
    } catch (err) {
      setMovieDetail({
        title: "Error",
        year: "",
        plot: err.message,
        genres: [],
        cast: [],
        directors: [],
        posterUrl: "",
      });
      setError(err.message);
    } finally {
      setMovieDetailLoading(false);
    }
  }

  async function handleGetRecs() {
    if (!userId) return;
    setRecsLoading(true);
    try {
      const data = await api(`/users/${userId}/recommendations`);
      setRecs(data);
      setError("");
    } catch (err) {
      setRecs([]);
      setError(err.message);
    } finally {
      setRecsLoading(false);
    }
  }

  async function handleFindPath() {
    if (!fromActor || !toActor) return;
    setPathLoading(true);
    setPathGraph(null);
    try {
      const result = await api(
        `/actors/path?fromId=${fromActor}&toId=${toActor}`,
      );
      setPathResult(result);
      if (result?.graph) {
        setPathGraph(result.graph);
      }
      setError("");
    } catch (err) {
      setPathResult({ found: false, hops: 0, error: err.message });
      setError(err.message);
    } finally {
      setPathLoading(false);
    }
  }

  useEffect(() => {
    loadVisNetwork();
  }, []);

  const healthCheck = async () => {
    try {
      const health = await api("/health");
      if (!health.cognodb) {
        setError(
          "CognoDB isn't reachable right now. Check your Neo4j environment variables.",
        );
      }
    } catch {
      setError("Couldn't reach the API. Is the backend running?");
    }
  };

  useEffect(() => {
    healthCheck();
  }, []);

  return (
    <>
      <header className="marquee">
        <h1 className="marquee-title">
          <span className="bulb" />
          REEL GRAPH
        </h1>
        <p className="marquee-sub">
          movie recommendations, powered by graph traversals on CognoDB
        </p>
        <nav className="tabs">
          {[
            { key: "browse", label: "Browse Films" },
            { key: "recs", label: "Recommended For You" },
            { key: "path", label: "Degrees of Separation" },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              className={`tab ${activeView === tab.key ? "active" : ""}`}
              onClick={() => setActiveView(tab.key)}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </header>

      <div className="perf-divider" />

      <main>
        {error && <div className="error-banner show">{error}</div>}

        {activeView === "browse" && (
          <section className="view active">
            <div className="two-col">
              <div className="panel">
                <h2 className="panel-title">Browse Films</h2>
                <div className="search-row">
                  <input
                    type="text"
                    value={movieSearch}
                    onChange={(e) => {
                      setMovieSearch(e.target.value);
                      debouncedLoadMovies(e.target.value.trim());
                    }}
                    placeholder='Search titles, e.g. "Andhadhun"...'
                  />
                </div>

                <div className="movie-grid">
                  {movieGridLoading ? (
                    <div className="loading-state">
                      <div className="spinner" />
                      Loading films…
                    </div>
                  ) : movies.length === 0 ? (
                    <div className="empty-state">
                      No films match that search.
                    </div>
                  ) : (
                    movies.map((movie) => (
                      <button
                        key={movie.id}
                        type="button"
                        className={`movie-card ${selectedMovieId === movie.id ? "selected" : ""}`}
                        onClick={() => selectMovie(movie.id)}
                      >
                        <img
                          src={movie.posterUrl}
                          alt={`${movie.title} poster`}
                          loading="lazy"
                        />
                        <div className="mc-body">
                          <p className="mc-title">{movie.title}</p>
                          <p className="mc-year">{movie.year}</p>
                        </div>
                      </button>
                    ))
                  )}
                </div>
              </div>

              <div className="panel">
                <h2 className="panel-title">Film Details &amp; Connections</h2>
                {movieDetailLoading ? (
                  <div className="loading-state">
                    <div className="spinner" />
                    Loading details…
                  </div>
                ) : selectedMovie ? (
                  <>
                    <img
                      className="detail-poster"
                      src={selectedMovie.posterUrl}
                      alt={`${selectedMovie.title} poster`}
                    />
                    <h3 style={{ margin: "0 0 4px", fontSize: "19px" }}>
                      {selectedMovie.title}{" "}
                      <span
                        style={{ color: "var(--text-muted)", fontWeight: 400 }}
                      >
                        ({selectedMovie.year})
                      </span>
                    </h3>
                    <div style={{ marginBottom: "8px" }}>
                      {(selectedMovie.genres || []).map((genre) => (
                        <span key={genre} className="pill">
                          {genre}
                        </span>
                      ))}
                    </div>
                    <p
                      style={{
                        fontSize: "13.5px",
                        color: "var(--text-muted)",
                        lineHeight: 1.5,
                      }}
                    >
                      {selectedMovie.plot}
                    </p>
                    <ul className="cast-list">
                      {(selectedMovie.directors || []).map((director) => (
                        <li key={director.id}>
                          <b>{director.name}</b> &mdash; Director
                        </li>
                      ))}
                      {(selectedMovie.cast || []).slice(0, 8).map((person) => (
                        <li key={person.id}>
                          <b>{person.name}</b>
                          {person.role ? ` as ${person.role}` : ""}
                        </li>
                      ))}
                    </ul>
                    <div style={{ clear: "both" }} />
                  </>
                ) : (
                  <div className="empty-state">
                    Select a film on the left to see its cast, director, genres,
                    and connected films.
                  </div>
                )}

                {movieGraph && (
                  <div style={{ marginTop: 16 }}>
                    <div ref={movieGraphRef} className="graph-box" />
                    <div className="graph-legend">
                      <span>
                        <i className="dot" style={{ background: "#e8b84b" }} />
                        Film
                      </span>
                      <span>
                        <i className="dot" style={{ background: "#4fd1c5" }} />
                        Person
                      </span>
                      <span>
                        <i className="dot" style={{ background: "#8a6fce" }} />
                        Genre
                      </span>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </section>
        )}

        {activeView === "recs" && (
          <section className="view active">
            <div className="panel">
              <h2 className="panel-title">Recommended For You</h2>
              <p
                style={{
                  color: "var(--text-muted)",
                  fontSize: "13.5px",
                  marginTop: "-6px",
                }}
              >
                A 3-hop traversal: films rated highly by other viewers who share
                your taste — and haven't shown up in your own ratings yet.
              </p>
              <div className="search-row">
                <select
                  value={userId}
                  onChange={(e) => setUserId(e.target.value)}
                >
                  {users.length === 0 ? (
                    <option value="">Loading viewers…</option>
                  ) : (
                    users.map((user) => (
                      <option key={user.id} value={user.id}>
                        {user.name}
                      </option>
                    ))
                  )}
                </select>
                <button
                  type="button"
                  className="primary"
                  onClick={handleGetRecs}
                  disabled={!userId || recsLoading}
                >
                  {recsLoading ? "Loading…" : "Get Recommendations"}
                </button>
              </div>

              {recsLoading ? (
                <div className="loading-state">
                  <div className="spinner" />
                  Finding films your taste-network loves…
                </div>
              ) : recs.length === 0 ? (
                <div className="empty-state">
                  Pick a viewer above to see what their taste-network is
                  watching.
                </div>
              ) : (
                recs.map((item) => (
                  <div key={item.movie.id} className="rec-row">
                    <img
                      src={item.movie.posterUrl}
                      alt={`${item.movie.title} poster`}
                    />
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 600, fontSize: "14px" }}>
                        {item.movie.title}{" "}
                        <span
                          style={{
                            color: "var(--text-muted)",
                            fontWeight: 400,
                          }}
                        >
                          ({item.movie.year})
                        </span>
                      </div>
                      <div>
                        {(item.movie.genres || []).map((genre) => (
                          <span
                            key={`${item.movie.id}-${genre}`}
                            className="pill"
                          >
                            {genre}
                          </span>
                        ))}
                      </div>
                      <div className="rec-because">
                        Because {item.becauseUsersAlsoLiked.join(", ")} also
                        rated it highly
                      </div>
                    </div>
                    <div className="rec-score">★ {item.score}</div>
                  </div>
                ))
              )}
            </div>
          </section>
        )}

        {activeView === "path" && (
          <section className="view active">
            <div className="panel">
              <h2 className="panel-title">Degrees of Separation</h2>
              <p
                style={{
                  color: "var(--text-muted)",
                  fontSize: "13.5px",
                  marginTop: "-6px",
                }}
              >
                Shortest path between two actors, hopping through shared films —
                the classic query a relational join table struggles with.
              </p>
              <div className="path-pickers">
                <div>
                  <label htmlFor="actor-from">From actor</label>
                  <select
                    id="actor-from"
                    value={fromActor}
                    onChange={(e) => setFromActor(e.target.value)}
                  >
                    {actors.length === 0 ? (
                      <option value="">Loading actors…</option>
                    ) : (
                      <>
                        <option value="">Select actor</option>
                        {actors.map((actor) => (
                          <option key={actor.id} value={actor.id}>
                            {actor.name}
                          </option>
                        ))}
                      </>
                    )}
                  </select>
                </div>
                <div>
                  <label htmlFor="actor-to">To actor</label>
                  <select
                    id="actor-to"
                    value={toActor}
                    onChange={(e) => setToActor(e.target.value)}
                  >
                    {actors.length === 0 ? (
                      <option value="">Loading actors…</option>
                    ) : (
                      <>
                        <option value="">Select actor</option>
                        {actors.map((actor) => (
                          <option key={actor.id} value={actor.id}>
                            {actor.name}
                          </option>
                        ))}
                      </>
                    )}
                  </select>
                </div>
                <button
                  type="button"
                  className="primary"
                  onClick={handleFindPath}
                  disabled={
                    pathLoading ||
                    !fromActor ||
                    !toActor ||
                    fromActor === toActor
                  }
                >
                  {pathLoading ? "Searching…" : "Find Path"}
                </button>
              </div>

              {pathResult && (
                <div className="path-result">
                  {pathResult.found ? (
                    <>
                      Connected in <b>{pathResult.hops}</b> hop
                      {pathResult.hops === 1 ? "" : "s"}.
                    </>
                  ) : (
                    <>No path found within 10 hops.</>
                  )}
                </div>
              )}

              {pathGraph && (
                <div style={{ marginTop: 16 }}>
                  <div ref={pathGraphRef} className="graph-box" />
                </div>
              )}
            </div>
          </section>
        )}
      </main>
    </>
  );
}

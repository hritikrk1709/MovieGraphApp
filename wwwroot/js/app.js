const API = "/api";

function $(sel) {
  return document.querySelector(sel);
}
function el(tag, props = {}, children = []) {
  const node = document.createElement(tag);
  Object.assign(node, props);
  for (const c of children) node.appendChild(c);
  return node;
}
function showError(message) {
  const banner = $("#error-banner");
  banner.textContent = message;
  banner.classList.add("show");
}
function clearError() {
  $("#error-banner").classList.remove("show");
}

async function api(path) {
  const res = await fetch(API + path);
  if (!res.ok) {
    let body = {};
    try {
      body = await res.json();
    } catch {
      /* ignore */
    }
    throw new Error(body.error || `Request failed (${res.status})`);
  }
  return res.json();
}

(async function checkHealth() {
  try {
    const health = await api("/health");
    if (!health.cognodb) {
      showError(
        "CognoDB isn't reachable right now, so live data can't load. Check your NEO4J__URI / NEO4J__USER / NEO4J__PASSWORD and that your instance is running.",
      );
    }
  } catch {
    showError("Couldn't reach the API. Is the backend running?");
  }
})();

document.querySelectorAll(".tab").forEach((tab) => {
  tab.addEventListener("click", () => {
    document
      .querySelectorAll(".tab")
      .forEach((t) => t.classList.remove("active"));
    document
      .querySelectorAll(".view")
      .forEach((v) => v.classList.remove("active"));
    tab.classList.add("active");
    $("#view-" + tab.dataset.view).classList.add("active");
  });
});

let selectedMovieId = null;

async function loadMovies(search) {
  const grid = $("#movie-grid");
  grid.innerHTML = `<div class="loading-state"><div class="spinner"></div>Loading films&hellip;</div>`;
  try {
    const movies = await api(
      `/movies${search ? "?search=" + encodeURIComponent(search) : ""}`,
    );
    clearError();
    if (movies.length === 0) {
      grid.innerHTML = `<div class="empty-state">No films match "${search}". Try another title.</div>`;
      return;
    }
    grid.innerHTML = "";
    for (const m of movies) {
      const card = el("button", { className: "movie-card", type: "button" });
      card.innerHTML = `
        <img src="${m.posterUrl}" alt="${m.title} poster" loading="lazy" />
        <div class="mc-body">
          <p class="mc-title">${m.title}</p>
          <p class="mc-year">${m.year}</p>
        </div>`;
      card.addEventListener("click", () => selectMovie(m.id, card));
      grid.appendChild(card);
    }
  } catch (err) {
    grid.innerHTML = `<div class="error-state">${err.message}</div>`;
    showError(err.message);
  }
}

async function selectMovie(id, cardEl) {
  selectedMovieId = id;
  document
    .querySelectorAll(".movie-card")
    .forEach((c) => c.classList.remove("selected"));
  if (cardEl) cardEl.classList.add("selected");

  const detail = $("#movie-detail");
  detail.innerHTML = `<div class="loading-state"><div class="spinner"></div>Loading details&hellip;</div>`;
  $("#movie-graph-wrap").style.display = "none";

  try {
    const [movie, graph] = await Promise.all([
      api(`/movies/${id}`),
      api(`/movies/${id}/graph`),
    ]);
    clearError();
    renderMovieDetail(movie);
    $("#movie-graph-wrap").style.display = "block";
    renderGraph("movie-graph", graph, movie.id);
  } catch (err) {
    detail.innerHTML = `<div class="error-state">${err.message}</div>`;
    showError(err.message);
  }
}

function renderMovieDetail(m) {
  const genrePills = m.genres
    .map((g) => `<span class="pill">${g}</span>`)
    .join("");
  const directors = m.directors
    .map((d) => `<li><b>${d.name}</b> &mdash; Director</li>`)
    .join("");
  const cast = m.cast
    .slice(0, 8)
    .map((c) => `<li><b>${c.name}</b>${c.role ? " as " + c.role : ""}</li>`)
    .join("");

  $("#movie-detail").innerHTML = `
    <img class="detail-poster" src="${m.posterUrl}" alt="${m.title} poster" />
    <h3 style="margin:0 0 4px; font-size:19px;">${m.title} <span style="color:var(--text-muted); font-weight:400;">(${m.year})</span></h3>
    <div style="margin-bottom:8px;">${genrePills}</div>
    <p style="font-size:13.5px; color:var(--text-muted); line-height:1.5;">${m.plot}</p>
    <ul class="cast-list">${directors}${cast}</ul>
    <div style="clear:both;"></div>
  `;
}

$("#movie-search").addEventListener(
  "input",
  debounce((e) => loadMovies(e.target.value.trim()), 300),
);

function debounce(fn, ms) {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), ms);
  };
}

async function loadUsers() {
  const select = $("#user-select");
  try {
    const users = await api("/users");
    clearError();
    select.innerHTML = users
      .map((u) => `<option value="${u.id}">${u.name}</option>`)
      .join("");
  } catch (err) {
    select.innerHTML = `<option>Couldn't load viewers</option>`;
    showError(err.message);
  }
}

$("#get-recs-btn").addEventListener("click", async () => {
  const userId = $("#user-select").value;
  const result = $("#recs-result");
  result.innerHTML = `<div class="loading-state"><div class="spinner"></div>Finding films your taste-network loves&hellip;</div>`;
  try {
    const recs = await api(`/users/${userId}/recommendations`);
    clearError();
    if (recs.length === 0) {
      result.innerHTML = `<div class="empty-state">No recommendations yet &mdash; this viewer's taste-network hasn't rated anything new highly.</div>`;
      return;
    }
    result.innerHTML = recs
      .map(
        (r) => `
      <div class="rec-row">
        <img src="${r.movie.posterUrl}" alt="${r.movie.title} poster" />
        <div style="flex:1;">
          <div style="font-weight:600; font-size:14px;">${r.movie.title} <span style="color:var(--text-muted); font-weight:400;">(${r.movie.year})</span></div>
          <div>${r.movie.genres.map((g) => `<span class="pill">${g}</span>`).join("")}</div>
          <div class="rec-because">Because ${r.becauseUsersAlsoLiked.join(", ")} also rated it highly</div>
        </div>
        <div class="rec-score">&#9733; ${r.score}</div>
      </div>
    `,
      )
      .join("");
  } catch (err) {
    result.innerHTML = `<div class="error-state">${err.message}</div>`;
    showError(err.message);
  }
});

async function loadActors() {
  try {
    const actors = await api("/actors");
    clearError();
    const opts = actors
      .map((a) => `<option value="${a.id}">${a.name}</option>`)
      .join("");
    $("#actor-from").innerHTML = opts;
    $("#actor-to").innerHTML = opts;
    if ($("#actor-from").querySelector('option[value="p_ranveer"]'))
      $("#actor-from").value = "p_ranveer";
    if ($("#actor-to").querySelector('option[value="p_ranbir"]'))
      $("#actor-to").value = "p_ranbir";
  } catch (err) {
    showError(err.message);
  }
}

$("#find-path-btn").addEventListener("click", async () => {
  const fromId = $("#actor-from").value;
  const toId = $("#actor-to").value;
  const resultEl = $("#path-result");
  const graphWrap = $("#path-graph-wrap");
  resultEl.innerHTML = `<div class="loading-state"><div class="spinner"></div>Searching the graph&hellip;</div>`;
  graphWrap.style.display = "none";

  try {
    const result = await api(`/actors/path?fromId=${fromId}&toId=${toId}`);
    clearError();
    if (!result.found) {
      resultEl.innerHTML = `<div class="empty-state">No path found within 10 hops &mdash; these two haven't crossed paths (yet) in this dataset.</div>`;
      return;
    }
    resultEl.innerHTML = `Connected in <b>${result.hops}</b> hop${result.hops === 1 ? "" : "s"}.`;
    graphWrap.style.display = "block";
    renderGraph("path-graph", result.graph, null);
  } catch (err) {
    resultEl.innerHTML = `<div class="error-state">${err.message}</div>`;
    showError(err.message);
  }
});

const GROUP_COLORS = {
  Movie: "#e8b84b",
  Person: "#4fd1c5",
  Genre: "#8a6fce",
};

function renderGraph(containerId, graph, focusId) {
  const container = document.getElementById(containerId);
  const nodes = new vis.DataSet(
    graph.nodes.map((n) => ({
      id: n.id,
      label: n.label,
      color: {
        background: GROUP_COLORS[n.group] || "#666",
        border: n.id === focusId ? "#fff" : GROUP_COLORS[n.group] || "#666",
        highlight: { background: GROUP_COLORS[n.group], border: "#fff" },
      },
      shape: n.group === "Movie" ? "dot" : "dot",
      size: n.id === focusId ? 26 : n.group === "Genre" ? 14 : 18,
      font: { color: "#f1efea", size: 13, face: "Inter" },
      borderWidth: n.id === focusId ? 3 : 1,
    })),
  );
  const edges = new vis.DataSet(
    graph.edges.map((e, i) => ({
      id: i,
      from: e.from,
      to: e.to,
      label: e.type.replace("_", " ").toLowerCase(),
      font: { color: "#9298a8", size: 9, strokeWidth: 0, align: "middle" },
      color: { color: "#2e3242", highlight: "#e8b84b" },
      arrows: { to: { enabled: false } },
      smooth: { type: "continuous" },
    })),
  );

  new vis.Network(
    container,
    { nodes, edges },
    {
      physics: {
        stabilization: true,
        barnesHut: { gravitationalConstant: -6000, springLength: 120 },
      },
      interaction: { hover: true, tooltipDelay: 100 },
      layout: { improvedLayout: true },
    },
  );
}

// ---------- init ----------
loadMovies();
loadUsers();
loadActors();

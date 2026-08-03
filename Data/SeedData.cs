namespace MovieGraphApp.Data;

public record MovieSeed(string Id, string Title, int Year, string Plot, string[] Genres);
public record PersonRoleSeed(string PersonId, string PersonName, string MovieId, string Role);
public record DirectorSeed(string PersonId, string PersonName, string MovieId);
public record UserSeed(string Id, string Name);
public record RatingSeed(string UserId, string MovieId, int Rating);


public static class SeedData
{
    public static readonly MovieSeed[] Movies =
    {
        new("m01", "Dilwale Dulhania Le Jayenge", 1995, "A young NRI man falls for a woman promised to another, and follows her to India to win her family's approval.", new[] { "Romance", "Drama" }),
        new("m02", "Kuch Kuch Hota Hai", 1998, "A love triangle unfolds across college years and a chance reunion years later.", new[] { "Romance", "Drama" }),
        new("m03", "Kabhi Khushi Kabhie Gham", 2001, "A family is torn apart over a marriage the patriarch disapproves of, and slowly finds its way back together.", new[] { "Drama", "Romance" }),
        new("m04", "My Name Is Khan", 2010, "A man with Asperger's undertakes a cross-country journey to meet the US President after 9/11 upends his family.", new[] { "Drama" }),
        new("m05", "Devdas", 2002, "A man returns from abroad to find his childhood love married off to another, and spirals into self-destruction.", new[] { "Drama", "Romance" }),
        new("m06", "Black", 2005, "A deafblind woman and her unconventional teacher form a bond that shapes her entire life.", new[] { "Drama" }),
        new("m07", "Bajirao Mastani", 2015, "An 18th-century Peshwa warrior falls for a warrior-princess, defying his family and court.", new[] { "Drama", "Romance", "History" }),
        new("m08", "Padmaavat", 2018, "A Rajput queen's kingdom is besieged by a ruthless sultan obsessed with her beauty.", new[] { "Drama", "History" }),
        new("m09", "3 Idiots", 2009, "Two friends search for their long-lost, free-spirited college roommate, recalling how he changed their lives.", new[] { "Comedy", "Drama" }),
        new("m10", "PK", 2014, "An alien stranded on Earth questions religious dogma with childlike honesty while searching for his way home.", new[] { "Comedy", "Drama", "Sci-Fi" }),
        new("m11", "Sanju", 2018, "The turbulent life story of a film star grappling with addiction, prison, and public redemption.", new[] { "Drama", "Biography" }),
        new("m12", "Munna Bhai M.B.B.S.", 2003, "A good-hearted gangster fakes his way into medical college and ends up teaching it a lesson in compassion.", new[] { "Comedy", "Drama" }),
        new("m13", "Zindagi Na Milegi Dobara", 2011, "Three friends on a bachelor road trip through Spain confront old wounds and buried fears.", new[] { "Drama", "Comedy" }),
        new("m14", "Gully Boy", 2019, "A young man from Mumbai's slums finds his voice, and his way out, through underground rap.", new[] { "Drama", "Musical" }),
        new("m15", "Dil Chahta Hai", 2001, "Three childhood friends drift apart and reunite as love and adulthood test their bond.", new[] { "Drama", "Comedy" }),
        new("m16", "Gangs of Wasseypur", 2012, "A multi-generational blood feud over coal-mafia turf consumes two families in small-town Bihar.", new[] { "Crime", "Drama" }),
        new("m17", "Dev D", 2009, "A modern, drug-fuelled retelling of a classic tragic romance set in Delhi and Mumbai's underbelly.", new[] { "Drama", "Romance" }),
        new("m18", "Raazi", 2018, "A young woman is married into a Pakistani military family to spy for India on the eve of the 1971 war.", new[] { "Thriller", "Drama" }),
        new("m19", "Andhadhun", 2018, "A blind pianist stumbles into a murder cover-up that may not be as blind to him as everyone assumes.", new[] { "Thriller", "Comedy" }),
        new("m20", "Barfi!", 2012, "A deaf-mute man's love story unfolds across two women in 1970s Darjeeling.", new[] { "Drama", "Romance", "Comedy" }),
        new("m21", "Yeh Jawaani Hai Deewani", 2013, "A free-spirited traveler and a studious homebody reconnect years after a life-changing trip.", new[] { "Romance", "Drama" }),
        new("m22", "Rockstar", 2011, "An aspiring musician chases heartbreak on purpose, believing it's the only way to make real art.", new[] { "Musical", "Drama", "Romance" }),
    };

    public static readonly DirectorSeed[] Directors =
    {
        new("p_adichopra", "Aditya Chopra", "m01"),
        new("p_kjohar", "Karan Johar", "m02"),
        new("p_kjohar", "Karan Johar", "m03"),
        new("p_kjohar", "Karan Johar", "m04"),
        new("p_bhansali", "Sanjay Leela Bhansali", "m05"),
        new("p_bhansali", "Sanjay Leela Bhansali", "m06"),
        new("p_bhansali", "Sanjay Leela Bhansali", "m07"),
        new("p_bhansali", "Sanjay Leela Bhansali", "m08"),
        new("p_hirani", "Rajkumar Hirani", "m09"),
        new("p_hirani", "Rajkumar Hirani", "m10"),
        new("p_hirani", "Rajkumar Hirani", "m11"),
        new("p_hirani", "Rajkumar Hirani", "m12"),
        new("p_zakhtar", "Zoya Akhtar", "m13"),
        new("p_zakhtar", "Zoya Akhtar", "m14"),
        new("p_fakhtar", "Farhan Akhtar", "m15"),
        new("p_kashyap", "Anurag Kashyap", "m16"),
        new("p_kashyap", "Anurag Kashyap", "m17"),
        new("p_gulzar", "Meghna Gulzar", "m18"),
        new("p_raghavan", "Sriram Raghavan", "m19"),
        new("p_basu", "Anurag Basu", "m20"),
        new("p_mukerji", "Ayan Mukerji", "m21"),
        new("p_ali", "Imtiaz Ali", "m22"),
    };

    public static readonly PersonRoleSeed[] Cast =
    {
        new("p_srk", "Shah Rukh Khan", "m01", "Raj"),
        new("p_kajol", "Kajol", "m01", "Simran"),
        new("p_srk", "Shah Rukh Khan", "m02", "Rahul"),
        new("p_kajol", "Kajol", "m02", "Anjali"),
        new("p_ranimukerji", "Rani Mukerji", "m02", "Tina"),
        new("p_srk", "Shah Rukh Khan", "m03", "Rahul"),
        new("p_kajol", "Kajol", "m03", "Anjali"),
        new("p_hrithik", "Hrithik Roshan", "m03", "Rohan"),
        new("p_srk", "Shah Rukh Khan", "m04", "Rizwan Khan"),
        new("p_kajol", "Kajol", "m04", "Mandira"),
        new("p_srk", "Shah Rukh Khan", "m05", "Devdas"),
        new("p_aishwarya", "Aishwarya Rai", "m05", "Paro"),
        new("p_madhuri", "Madhuri Dixit", "m05", "Chandramukhi"),
        new("p_amitabh", "Amitabh Bachchan", "m06", "Debraj Sahai"),
        new("p_ranimukerji", "Rani Mukerji", "m06", "Michelle"),
        new("p_ranveer", "Ranveer Singh", "m07", "Bajirao"),
        new("p_deepika", "Deepika Padukone", "m07", "Mastani"),
        new("p_priyanka", "Priyanka Chopra", "m07", "Kashibai"),
        new("p_deepika", "Deepika Padukone", "m08", "Padmavati"),
        new("p_ranveer", "Ranveer Singh", "m08", "Alauddin Khilji"),
        new("p_shahid", "Shahid Kapoor", "m08", "Maharawal Ratan Singh"),
        new("p_aamir", "Aamir Khan", "m09", "Rancho"),
        new("p_madhavan", "R. Madhavan", "m09", "Farhan"),
        new("p_sharman", "Sharman Joshi", "m09", "Raju"),
        new("p_aamir", "Aamir Khan", "m10", "PK"),
        new("p_anushka", "Anushka Sharma", "m10", "Jaggu"),
        new("p_ranbir", "Ranbir Kapoor", "m11", "Sanjay Dutt"),
        new("p_vicky", "Vicky Kaushal", "m11", "Kamlesh"),
        new("p_sanjaydutt", "Sanjay Dutt", "m12", "Murli Prasad Sharma"),
        new("p_arshad", "Arshad Warsi", "m12", "Circuit"),
        new("p_hrithik", "Hrithik Roshan", "m13", "Arjun"),
        new("p_farhanakhtar", "Farhan Akhtar", "m13", "Imraan"),
        new("p_abhaydeol", "Abhay Deol", "m13", "Kabir"),
        new("p_katrina", "Katrina Kaif", "m13", "Laila"),
        new("p_ranveer", "Ranveer Singh", "m14", "Murad"),
        new("p_alia", "Alia Bhatt", "m14", "Safeena"),
        new("p_aamir", "Aamir Khan", "m15", "Akash"),
        new("p_saifalikhan", "Saif Ali Khan", "m15", "Sameer"),
        new("p_akshaye", "Akshaye Khanna", "m15", "Sid"),
        new("p_manojbajpayee", "Manoj Bajpayee", "m16", "Sardar Khan"),
        new("p_nawazuddin", "Nawazuddin Siddiqui", "m16", "Faizal Khan"),
        new("p_abhaydeol", "Abhay Deol", "m17", "Dev"),
        new("p_mahiegill", "Mahie Gill", "m17", "Paro"),
        new("p_alia", "Alia Bhatt", "m18", "Sehmat"),
        new("p_vicky", "Vicky Kaushal", "m18", "Iqbal"),
        new("p_ayushmann", "Ayushmann Khurrana", "m19", "Akash"),
        new("p_tabu", "Tabu", "m19", "Simi"),
        new("p_radhika", "Radhika Apte", "m19", "Sophie"),
        new("p_ranbir", "Ranbir Kapoor", "m20", "Barfi"),
        new("p_priyanka", "Priyanka Chopra", "m20", "Jhilmil"),
        new("p_ileana", "Ileana D'Cruz", "m20", "Shruti"),
        new("p_ranbir", "Ranbir Kapoor", "m21", "Kabir 'Bunny'"),
        new("p_deepika", "Deepika Padukone", "m21", "Naina"),
        new("p_ranbir", "Ranbir Kapoor", "m22", "Janardhan 'Jordan'"),
        new("p_nargis", "Nargis Fakhri", "m22", "Heer"),
    };

    public static readonly UserSeed[] Users =
    {
        new("u01", "Priya"),
        new("u02", "Rohan"),
        new("u03", "Ananya"),
        new("u04", "Vikram"),
        new("u05", "Aisha"),
        new("u06", "Karan"),
        new("u07", "Neha"),
        new("u08", "Arjun"),
        new("u09", "Meera"),
        new("u10", "Rahul"),
    };

    public static readonly RatingSeed[] Ratings =
    {
        //  SRK-Kajol romance fans
        new("u01", "m01", 5), new("u01", "m02", 5), new("u01", "m03", 4), new("u01", "m04", 4), new("u01", "m05", 4), new("u01", "m20", 4),
        new("u02", "m01", 4), new("u02", "m02", 5), new("u02", "m03", 5), new("u02", "m05", 4), new("u02", "m21", 4),

        // Bhansali epic fans
        new("u03", "m05", 5), new("u03", "m06", 5), new("u03", "m07", 5), new("u03", "m08", 4), new("u03", "m03", 4),
        new("u04", "m06", 4), new("u04", "m07", 5), new("u04", "m08", 5), new("u04", "m16", 4), new("u04", "m01", 3),

        //  Hirani comedy-drama fans
        new("u05", "m09", 5), new("u05", "m10", 5), new("u05", "m11", 4), new("u05", "m12", 4), new("u05", "m15", 4),
        new("u06", "m09", 5), new("u06", "m11", 5), new("u06", "m12", 4), new("u06", "m19", 4), new("u06", "m10", 4),

        //  Kapoor/Padukone youth-drama fans
        new("u07", "m20", 5), new("u07", "m21", 5), new("u07", "m22", 5), new("u07", "m07", 4), new("u07", "m14", 4),
        new("u08", "m21", 5), new("u08", "m22", 4), new("u08", "m13", 5), new("u08", "m14", 4), new("u08", "m20", 4),

        //  crime/thriller fans
        new("u09", "m16", 5), new("u09", "m17", 5), new("u09", "m18", 4), new("u09", "m19", 4), new("u09", "m09", 4),
        new("u10", "m16", 4), new("u10", "m18", 4), new("u10", "m19", 5), new("u10", "m17", 5), new("u10", "m13", 4),
    };
}

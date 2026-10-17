namespace HospitalSystem.Data
{
    // The hospital's rooms: 2 private rooms (1 bed), 2 semi-private rooms (2 beds), 2 wards (3 beds)
    // and 2 ICU rooms (2 beds), 16 beds in all. Data/schema.sql seeds the same layout;
    // SchemaMigrator moves older databases onto it.
    public static class BedLayout
    {
        public const string Private = "Private";
        public const string SemiPrivate = "Semi-Private";
        public const string Ward = "Ward";
        public const string Icu = "ICU";

        public static readonly (string Room, string Bed, string Ward)[] Beds =
        {
            ("101", "1", Private),
            ("102", "1", Private),
            ("103", "1", SemiPrivate), ("103", "2", SemiPrivate),
            ("104", "1", SemiPrivate), ("104", "2", SemiPrivate),
            ("105", "1", Ward), ("105", "2", Ward), ("105", "3", Ward),
            ("106", "1", Ward), ("106", "2", Ward), ("106", "3", Ward),
            ("107", "1", Icu), ("107", "2", Icu),
            ("108", "1", Icu), ("108", "2", Icu)
        };

        // Daily room rates for the charge schedule (editable afterwards under Billing).
        public const decimal WardRate = 1500m;
        public const decimal SemiPrivateRate = 2250m;
        public const decimal PrivateRate = 3000m;
        public const decimal IcuRate = 8000m;
    }
}

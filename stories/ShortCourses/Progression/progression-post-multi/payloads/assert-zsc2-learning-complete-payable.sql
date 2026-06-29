SELECT TOP 1 i.[IsPayable]
FROM [Domain].[ShortCourseInstalment] i
JOIN [Domain].[ShortCourseEarningsProfile] ep ON ep.[EarningsProfileId] = i.[EarningsProfileId]
JOIN [Domain].[ShortCourseEpisode] e ON e.[Key] = ep.[EpisodeKey]
WHERE e.[StartDate] = '2026-02-01'
AND i.[Type] = 'LearningComplete'

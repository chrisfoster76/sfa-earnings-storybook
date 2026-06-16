SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseInstalment] i
JOIN [Domain].[ShortCourseEarningsProfile] ep ON ep.[EarningsProfileId] = i.[EarningsProfileId]
JOIN [Domain].[ShortCourseEpisode] e ON e.[Key] = ep.[EpisodeKey]
WHERE e.[Ukprn] = 10005078
AND i.[Type] = 'ThirtyPercentLearningComplete'

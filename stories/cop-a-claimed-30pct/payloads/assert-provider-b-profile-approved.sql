SELECT ep.[IsApproved]
FROM [Domain].[ShortCourseEarningsProfile] ep
JOIN [Domain].[ShortCourseEpisode] e ON e.[Key] = ep.[EpisodeKey]
WHERE e.[Ukprn] = 10005078

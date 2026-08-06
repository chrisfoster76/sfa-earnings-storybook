-- EmployerType: 0 = NonLevy, 1 = Levy
SELECT sce.[EmployerType] AS EmployerType
FROM [Domain].[ShortCourseEpisode] sce
INNER JOIN [Domain].[ShortCourseLearning] scl ON scl.[LearningKey] = sce.[LearningKey]
WHERE scl.[Uln] = '23456734'
  AND sce.[Ukprn] = 10005077
  AND sce.[IsRemoved] = 0

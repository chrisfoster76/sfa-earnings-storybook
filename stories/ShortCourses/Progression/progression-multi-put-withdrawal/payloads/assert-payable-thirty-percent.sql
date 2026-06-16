SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseInstalment]
WHERE [Type] = 'ThirtyPercentLearningComplete'
  AND [IsPayable] = 1

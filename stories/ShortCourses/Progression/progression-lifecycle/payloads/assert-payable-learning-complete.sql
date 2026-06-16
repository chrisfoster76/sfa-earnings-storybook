SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseInstalment]
WHERE [Type] = 'LearningComplete'
  AND [IsPayable] = 1

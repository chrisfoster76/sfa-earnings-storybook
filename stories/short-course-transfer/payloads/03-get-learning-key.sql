SELECT TOP 1 l.[Key] AS LearnerKey
FROM ShortCourseLearning scl
INNER JOIN Learner l ON l.[Key] = scl.[LearnerKey]
INNER JOIN ShortCourseEpisode sce ON sce.[LearningKey] = scl.[Key]
WHERE l.Uln = 23456735
  AND sce.UKPRN = 10005077
  AND sce.TrainingCode = 'ZSC00001'
  AND sce.StartDate = '2025-08-01'

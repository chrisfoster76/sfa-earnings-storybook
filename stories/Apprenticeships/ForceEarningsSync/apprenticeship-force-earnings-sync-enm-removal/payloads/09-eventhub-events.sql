-- Events captured by EventHub for this story's learner (local events only; the storybook wipes them before each run)
-- E&M releases carry the aim ref in CourseCode; on-programme carries the standard code
SELECT
    COUNT(CASE WHEN JSON_VALUE([Payload], '$.Command.Training.CourseType') = '1' THEN 1 END) AS [OnProgrammeReleases],
    COUNT(CASE WHEN JSON_VALUE([Payload], '$.Command.Training.CourseCode') = '60269533' THEN 1 END) AS [MathsReleases],
    COUNT(CASE WHEN JSON_VALUE([Payload], '$.Command.Training.CourseCode') = '60347316' THEN 1 END) AS [EnglishReleases],
    COUNT(CASE WHEN JSON_VALUE([Payload], '$.Command.Learner.LearnerKey') = '00000000-0000-0000-0000-000000000000' THEN 1 END) AS [ReleasesWithEmptyLearnerKey]
FROM [dbo].[ReceivedEvents]
WHERE [Environment] = 'local'
  AND [EventType] = 'GrowthAndSkillsPaymentsRecalculatedEvent'
  AND JSON_VALUE([Payload], '$.Command.Learner.ULN') = '44444491'

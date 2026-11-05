-- Events captured by EventHub for this story's learner (local events only; the storybook wipes them before each run)
SELECT
    COUNT(CASE WHEN [EventType] = 'GrowthAndSkillsPaymentsRecalculatedEvent' AND JSON_VALUE([Payload], '$.Command.Learner.ULN') = '44444489' AND JSON_VALUE([Payload], '$.Command.Training.CourseType') = '1' THEN 1 END) AS [OnProgrammeReleases],
    COUNT(CASE WHEN [EventType] = 'GrowthAndSkillsPaymentsRecalculatedEvent' AND JSON_VALUE([Payload], '$.Command.Learner.ULN') = '44444489' AND JSON_VALUE([Payload], '$.Command.Learner.LearnerKey') = '00000000-0000-0000-0000-000000000000' THEN 1 END) AS [ReleasesWithEmptyLearnerKey]
FROM [dbo].[ReceivedEvents]
WHERE [Environment] = 'local'

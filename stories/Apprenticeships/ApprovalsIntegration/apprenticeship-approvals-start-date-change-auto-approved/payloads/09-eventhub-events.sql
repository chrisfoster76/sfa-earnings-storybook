-- Events captured by EventHub for this story's learner (local events only; the storybook wipes them before each run)
SELECT
    COUNT(CASE WHEN [EventType] = 'ApprovedLearningUpdatedEvent' AND JSON_VALUE([Payload], '$.ApprenticeshipId') = '200030' AND JSON_VALUE([Payload], '$.Changes[0].ChangeType') = 'StartDate' AND JSON_VALUE([Payload], '$.Changes[0].Data.New') = '2025-09-15' THEN 1 END) AS [ApprovedStartDateTo15th],
    COUNT(CASE WHEN [EventType] = 'GrowthAndSkillsPaymentsRecalculatedEvent' AND JSON_VALUE([Payload], '$.Command.Learner.ULN') = '44444480' AND JSON_VALUE([Payload], '$.Command.Training.CourseType') = '1' AND JSON_VALUE([Payload], '$.Command.Training.StartDate') LIKE '2025-09-15%' THEN 1 END) AS [OnProgrammePaymentsFrom15th],
    COUNT(CASE WHEN [EventType] = 'WithholdPaymentsForLearningEvent' AND JSON_VALUE([Payload], '$.ULN') = '44444480' THEN 1 END) AS [Withholds]
FROM [dbo].[ReceivedEvents]
WHERE [Environment] = 'local'

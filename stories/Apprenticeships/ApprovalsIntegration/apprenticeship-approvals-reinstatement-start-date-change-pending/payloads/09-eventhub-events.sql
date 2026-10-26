-- Events captured by EventHub for this story's learner (local events only; the storybook wipes them before each run)
SELECT
    COUNT(CASE WHEN [EventType] = 'LearningReinstatedEvent' AND JSON_VALUE([Payload], '$.ApprenticeshipId') = '200035' THEN 1 END) AS [Reinstatements],
    COUNT(CASE WHEN [EventType] = 'WithholdPaymentsForLearningEvent' AND JSON_VALUE([Payload], '$.ULN') = '44444485' THEN 1 END) AS [Withholds],
    COUNT(CASE WHEN [EventType] = 'ApprovedLearningUpdatedEvent' AND JSON_VALUE([Payload], '$.ApprenticeshipId') = '200035' THEN 1 END) AS [ApprovedLearningUpdates]
FROM [dbo].[ReceivedEvents]
WHERE [Environment] = 'local'

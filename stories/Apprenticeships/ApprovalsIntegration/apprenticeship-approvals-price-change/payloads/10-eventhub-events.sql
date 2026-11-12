-- Events captured by EventHub for this story's learner (local events only; the storybook wipes them before each run)
SELECT
    COUNT(CASE WHEN [EventType] = 'WithholdPaymentsForLearningEvent' AND JSON_VALUE([Payload], '$.ULN') = '44444481' THEN 1 END) AS [Withholds],
    COUNT(CASE WHEN [EventType] = 'ApprovedLearningUpdatedEvent' AND JSON_VALUE([Payload], '$.ApprenticeshipId') = '200031' THEN 1 END) AS [ApprovedLearningUpdates],
    COUNT(CASE WHEN [EventType] = 'ApprovedLearningUpdatedEvent' AND JSON_VALUE([Payload], '$.ApprenticeshipId') = '200031'
        AND JSON_VALUE([Payload], '$.Changes[0].ChangeType') IS NULL
        AND TRY_CAST(JSON_VALUE([Payload], '$.Prices[0].TrainingPrice') AS DECIMAL(18,2)) = 7200
        AND TRY_CAST(JSON_VALUE([Payload], '$.Prices[0].AssessmentPrice') AS DECIMAL(18,2)) = 1800
        AND JSON_VALUE([Payload], '$.Prices[0].EffectiveFrom') LIKE '2025-09-01%'
        AND JSON_VALUE([Payload], '$.Prices[1].EffectiveFrom') IS NULL THEN 1 END) AS [ApprovedPricesEchoed],
    COUNT(CASE WHEN [EventType] = 'EpisodeApprovedEvent' THEN 1 END) AS [EpisodeApprovals], -- not filterable by learner here; the wipe scopes it to this story
    COUNT(CASE WHEN [EventType] = 'GrowthAndSkillsPaymentsRecalculatedEvent' AND JSON_VALUE([Payload], '$.Command.Learner.ULN') = '44444481'
        AND JSON_VALUE([Payload], '$.Command.Training.CourseType') = '1'
        AND TRY_CAST(JSON_VALUE([Payload], '$.Command.Earnings[0].PricePeriods[0].Price') AS DECIMAL(18,2)) = 9000 THEN 1 END) AS [OnProgrammePaymentsAt9000]
FROM [dbo].[ReceivedEvents]
WHERE [Environment] = 'local'

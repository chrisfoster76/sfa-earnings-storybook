-- Support's toggle (SqlScripts/SetForceApprenticeshipEarningsSync.sql in das-apprenticeships), scoped to this story's episode
SET NOCOUNT ON;

UPDATE [ApprenticeshipEpisode]
SET [ForceEarningsSync] = 1
WHERE [ApprovalsApprenticeshipId] = 200042;

SELECT [ForceEarningsSync] AS ForceEarningsSync
FROM [ApprenticeshipEpisode]
WHERE [ApprovalsApprenticeshipId] = 200042;

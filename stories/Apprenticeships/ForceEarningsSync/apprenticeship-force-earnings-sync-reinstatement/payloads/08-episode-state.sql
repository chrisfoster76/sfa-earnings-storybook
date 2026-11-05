SELECT
    ae.[IsRemoved] AS IsRemoved,
    ae.[IsApproved] AS IsApproved
FROM [ApprenticeshipEpisode] ae
WHERE ae.[ApprovalsApprenticeshipId] = 200040

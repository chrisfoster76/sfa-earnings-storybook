SELECT
    ae.[IsApproved] AS IsApproved,
    ae.[IsRemoved] AS IsRemoved
FROM [ApprenticeshipEpisode] ae
WHERE ae.[ApprovalsApprenticeshipId] = 900001

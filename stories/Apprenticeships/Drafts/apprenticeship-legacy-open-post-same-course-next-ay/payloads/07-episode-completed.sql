SELECT
    ae.[IsApproved] AS IsApproved,
    CONVERT(VARCHAR(10), ae.[CompletionDate], 120) AS CompletionDate
FROM [ApprenticeshipEpisode] ae
WHERE ae.[ApprovalsApprenticeshipId] = 700200

SELECT ISNULL(CONVERT(VARCHAR(10), ae.[WithdrawalDate], 23), 'none') AS WithdrawalDate
FROM [Domain].[ApprenticeshipEpisode] ae

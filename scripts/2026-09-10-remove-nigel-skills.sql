-- 2026-09-10 — remove Nigel's three personal commercial skills from the portal's skill store.
--
-- Nigel: "My skills are only client and contract related — mainly architect/CA. Never for
-- tender, suppliers, staff or anyone outside my role." They were seeded into the portal and
-- attached area-wide, which is what sent Jeremy's Claude into dispute mode on routine work
-- (docs/ai/12-connector-weakness-audit.md). Nigel holds the source copies in his own Claude
-- environment; the portal keeps nothing. Attachments were already cleared through the
-- connector on 2026-09-10; the DELETE below is belt and braces.
--
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i scripts/2026-09-10-remove-nigel-skills.sql -b
SET NOCOUNT ON;

DECLARE @keys TABLE ([SkillKey] nvarchar(128) NOT NULL);
INSERT INTO @keys VALUES
    (N'commercial-director'),
    (N'commercial-director-mistake-prevention'),
    (N'nigel-commercial-doctrine');

BEGIN TRANSACTION;

DELETE a FROM [dbo].[AiActionSkills] a JOIN @keys k ON k.[SkillKey] = a.[SkillKey];
PRINT CONCAT('AiActionSkills removed: ', @@ROWCOUNT);

DELETE r FROM [dbo].[SkillRevisions] r JOIN @keys k ON k.[SkillKey] = r.[SkillKey];
PRINT CONCAT('SkillRevisions removed: ', @@ROWCOUNT);

DELETE r FROM [dbo].[SkillReferences] r JOIN @keys k ON k.[SkillKey] = r.[SkillKey];
PRINT CONCAT('SkillReferences removed: ', @@ROWCOUNT);

DELETE s FROM [dbo].[Skills] s JOIN @keys k ON k.[SkillKey] = s.[SkillKey];
PRINT CONCAT('Skills removed: ', @@ROWCOUNT);

COMMIT TRANSACTION;

SELECT [SkillKey], [AgentKey], [Version], [IsActive] FROM [dbo].[Skills] ORDER BY [AgentKey], [SkillKey];

-- =====================================================================================
-- James Everitt, Monday 31 August 2026 (8h, INT-PLS, Approved, £250) — move from
-- Woodhouse (JBB-2026-004) to Ravenswood Ave (JBB-2026-003).  2026-09-07, Jeremy's ask.
--
-- FALLBACK ONLY. The proper route is the new Move… action on the Labour tab (or the
-- connector's move_worker_day), which does exactly this under the FD's own name with the
-- audit rows written by the handler. Run this only if that build is not yet deployed and
-- the move cannot wait — it does the same thing by hand, in one transaction:
--   1. Guards: exactly one Approved row for the worker on 2026-08-31 on Woodhouse with the
--      expected hours/code/£; no settlement cover spanning the day for his counterparty; no
--      Xero coding run recorded for his August. (Jeremy: week not signed off, not settled.)
--   2. Re-points the timesheet's ProjectId (and a linked SiteAttendance row, if any) to
--      Ravenswood. Date, hours, cost code, status and the approval snapshot are untouched.
--   3. Writes the two LabourDayMoved (41) audit rows the handler would write — one on each
--      project — under the actor email set below.
-- Every guard RAISERRORs (severity 16) so -b stops and the transaction rolls back.
-- =====================================================================================
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Actor      nvarchar(256) = N'consulting@yourbusiness.today';   -- who is running this
DECLARE @Reason     nvarchar(512) = N'Approved on Woodhouse in error — his timesheet message for the week confirms Surbiton (Ravenswood Ave) for the Monday; Dorking was the Saturday and Sunday. Moved by script at the FD''s request.';
DECLARE @FromProject nvarchar(64) = N'c16a737d8e1347f28917183b77360f1d';   -- JBB-2026-004 Woodhouse
DECLARE @ToProject   nvarchar(64) = N'3bf6dcfa81764a248138fb5fd357aa84';   -- JBB-2026-003 Ravenswood Ave
DECLARE @Day         datetimeoffset = '2026-08-31T00:00:00+00:00';

DECLARE @WorkerId nvarchar(64) = (SELECT TOP 1 WorkerId FROM Workers WHERE Name = N'James Everitt');
IF @WorkerId IS NULL RAISERROR('Guard: worker "James Everitt" not found.', 16, 1);

DECLARE @TimesheetId nvarchar(64), @Hours decimal(18,2), @CostCode nvarchar(32), @Cost decimal(18,2), @Attendance nvarchar(64);
SELECT @TimesheetId = TimesheetId, @Hours = Hours, @CostCode = CostCode, @Cost = CostAmount, @Attendance = SiteAttendanceId
FROM Timesheets
WHERE WorkerId = @WorkerId AND ProjectId = @FromProject AND WorkedOn = @Day AND Status = 1;   -- 1 = Approved

IF @TimesheetId IS NULL
    RAISERROR('Guard: no Approved timesheet for James Everitt on 2026-08-31 on Woodhouse.', 16, 1);
IF (SELECT COUNT(*) FROM Timesheets WHERE WorkerId = @WorkerId AND ProjectId = @FromProject AND WorkedOn = @Day AND Status = 1) <> 1
    RAISERROR('Guard: more than one Approved row on that day — resolve by hand.', 16, 1);
IF @Hours <> 8 OR @CostCode <> N'INT-PLS' OR @Cost <> 250
    RAISERROR('Guard: the row is not 8h / INT-PLS / £250 as Jeremy described — check before moving.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM Projects WHERE ProjectId = @ToProject AND Reference = N'JBB-2026-003')
    RAISERROR('Guard: destination is not JBB-2026-003.', 16, 1);

DECLARE @Counterparty nvarchar(64) = (SELECT COALESCE(SubcontractorId, CASE WHEN IsSoleTrader = 1 THEN WorkerId ELSE N'' END) FROM Workers WHERE WorkerId = @WorkerId);
IF @Counterparty <> N'' AND EXISTS (
    SELECT 1 FROM XeroLineTimesheetCovers
    WHERE SubcontractorId = @Counterparty AND ProjectId = @FromProject AND PeriodStart <= @Day AND PeriodEnd > @Day)
    RAISERROR('Guard: an invoice line is marked as covering this day on Woodhouse — settled; unmark it or post a variance instead.', 16, 1);
IF EXISTS (
    SELECT TOP 1 1 FROM XeroCodingRuns
    WHERE WorkerId = @WorkerId AND Month = '2026-08-01T00:00:00+00:00' AND Outcome IN (0, 1)   -- BillRecoded, DraftStaged
    ORDER BY RunAt DESC)
    RAISERROR('Guard: his August has been coded to Xero — reset the coding outcome first.', 16, 1);

BEGIN TRANSACTION;

UPDATE Timesheets SET ProjectId = @ToProject WHERE TimesheetId = @TimesheetId;
IF @Attendance IS NOT NULL AND @Attendance <> N''
    UPDATE SiteAttendances SET ProjectId = @ToProject WHERE SiteAttendanceId = @Attendance;

DECLARE @DayText nvarchar(400) = N'James Everitt Mon 31 Aug 2026 (' + CAST(@Hours AS nvarchar(20)) + N'h on ' + @CostCode
    + N', Approved, £' + FORMAT(@Cost, 'N2') + N' approved)';
INSERT INTO AuditEvents (AuditEventId, OccurredAt, ActorEmail, EventType, Pathway, ProjectId, RecordType, RecordId, RecordReference, ConversationId, EmailMessageId, InternetMessageId, WebLink, Detail)
VALUES
    (LOWER(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', '')), SYSDATETIMEOFFSET(), @Actor, 41, N'', @FromProject, NULL, NULL, N'', NULL, NULL, NULL, NULL,
     LEFT(@DayText + N' moved to JBB-2026-003 Ravenswood Ave. Reason given: ' + @Reason, 1024)),
    (LOWER(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', '')), SYSDATETIMEOFFSET(), @Actor, 41, N'', @ToProject, NULL, NULL, N'', NULL, NULL, NULL, NULL,
     LEFT(@DayText + N' moved here from JBB-2026-004 Woodhouse. Reason given: ' + @Reason, 1024));

COMMIT TRANSACTION;

SELECT TimesheetId, ProjectId, WorkedOn, Hours, CostCode, Status, RateApplied, CostAmount, ApprovedByEmail, ApprovedAt
FROM Timesheets WHERE TimesheetId = @TimesheetId;

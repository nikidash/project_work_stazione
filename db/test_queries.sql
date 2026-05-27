------------------------------------------------------
-- test queries
------------------------------------------------------
-- This script uses manually assigned IDs for sectors and semaphores
-- to guarantee foreign key relationships are met in dependent tables.

-- 1. PREREQUISITE CREATION (Ensuring all FKs exist)

-- Create Sectors used for Switches (101, 102, 103) and Maintenance (200)
INSERT INTO sectors (id, length, is_available, is_stop, platform_number)
VALUES (10, 150, TRUE, TRUE, 1),  -- Platform 1 (Used by Trains test)
(20, 150, TRUE, TRUE, 2),  -- Platform 2 (Used by TrainsLog test)
(30, 150, TRUE, TRUE, 3),  -- Platform 3 (Used for later sector delete test)
(101, 100, TRUE, FALSE, NULL), -- Switch Input
(102, 150, TRUE, FALSE, NULL), -- Switch Straight
(103, 150, TRUE, FALSE, NULL), -- Switch Deviation
(200, 50, TRUE, FALSE, NULL)   -- Maintenance Platform
RETURNING id;

-- Create Semaphore (ID 50) needed for Switches
INSERT INTO semaphores (id, is_working, is_green, is_deviating)
VALUES (50, TRUE, TRUE, FALSE)
RETURNING id;

-- Create Switches (ID 1000)
INSERT INTO "switches" (id, input_id, straight_id, deviation_id, semaphore_id, is_working, is_deviating)
VALUES (1000, 101, 102, 103, 50, TRUE, FALSE)
RETURNING id;

-- Create Trains record (Will be deleted later)
INSERT INTO Trains (id, trainNumber, length, origin, destination, trainType, scheduledArrivalTime, scheduledDepartureTime, isTransit, assignedPlatform)
VALUES (3000, 'T801', 10, 'L1', 'R', 'R', '2025-12-15 08:00:00', '2025-12-15 08:15:00', FALSE, 1)
RETURNING id;

-- Create TrainsLog record (ID 5000) needed for Maintenance
INSERT INTO TrainsLog (id, trainNumber, length, origin, destination, trainType, scheduledArrivalTime, scheduledDepartureTime, isTransit, actualDepartureTime, actualArrivalTime, assignedPlatform)
VALUES (5000, 'T902', 12, 'R2', 'L', 'F', '2025-12-15 09:00:00', '2025-12-15 09:05:00', TRUE, '2025-12-15 09:06:00', '2025-12-15 09:00:00', 2)
RETURNING id;

-- Create Maintenance record (ID 7000)
INSERT INTO Maintenance (id, trainId, maintenanceStart, maintenanceEnd, maintenancePlatform)
VALUES (7000, 5000, '2025-12-15 10:00:00', '2025-12-15 11:30:00', 200)
RETURNING id;

-- 2. CRUD TESTS (READ, UPDATE, DELETE)

-- sectors
SELECT id, length, is_available, is_stop, platform_number FROM sectors
WHERE platform_number = 1; -- Read

UPDATE sectors
SET is_available = FALSE, length = 160
WHERE id = 10
RETURNING *; -- Update

-- semaphores
SELECT id, is_working, is_green, is_deviating FROM semaphores
WHERE id = 50; -- Read

UPDATE semaphores
SET is_green = FALSE, is_working = TRUE
WHERE id = 50
RETURNING *; -- Update

-- switches
SELECT * FROM "switches"
WHERE id = 1000; -- Read

UPDATE "switches"
SET is_deviating = TRUE, is_working = TRUE
WHERE id = 1000
RETURNING *; -- Update

-- Trains
SELECT * FROM Trains
WHERE id = 3000; -- Read (using manually assigned ID)

UPDATE Trains
SET assignedPlatform = 2, scheduledDepartureTime = '2025-12-15 08:20:00'
WHERE id = 3000
RETURNING *; -- Update

-- TrainsLog
SELECT * FROM TrainsLog
WHERE id = 5000; -- Read

UPDATE TrainsLog
SET actualDepartureTime = '2025-12-15 09:08:00'
WHERE id = 5000
RETURNING *; -- Update

-- Maintenance
SELECT * FROM Maintenance
WHERE id = 7000; -- Read

UPDATE Maintenance
SET maintenanceEnd = '2025-12-15 12:00:00'
WHERE id = 7000
RETURNING *; -- Update

-- 3. CLEANUP (DELETEs) - Must run in reverse dependency order

-- Delete Maintenance first (Depends on TrainsLog and sectors)
DELETE FROM Maintenance
WHERE id = 7000
RETURNING id;

-- Delete Switches (Depends on sectors and semaphores)
DELETE FROM "switches"
WHERE id = 1000
RETURNING id;

-- Delete TrainsLog (No dependencies, but TrainsLog is a log)
DELETE FROM TrainsLog
WHERE id = 5000
RETURNING id;

-- Delete Trains (No dependencies)
DELETE FROM Trains
WHERE id = 3000
RETURNING id;

-- Delete Semaphores (No dependencies)
DELETE FROM semaphores
WHERE id = 50
RETURNING id;

-- Delete Sectors (Must be last)
DELETE FROM sectors
WHERE id IN (10, 20, 30, 101, 102, 103, 200)
RETURNING id;
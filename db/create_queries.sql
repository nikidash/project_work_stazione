CREATE TABLE IF NOT EXISTS sectors (
  id SERIAL PRIMARY KEY,
  length INT NOT NULL,
  is_available BOOL NOT NULL,
  is_stop BOOL NOT NULL,
  platform_number INT UNIQUE CHECK (platform_number BETWEEN 1 AND 4)
);

CREATE TABLE IF NOT EXISTS semaphores (
  id SERIAL PRIMARY KEY,
  is_working BOOL NOT NULL,
  is_green BOOL NOT NULL,
  is_deviating BOOL NOT NULL
);

CREATE TABLE IF NOT EXISTS "switches"(
  id SERIAL PRIMARY KEY,
  input_id INT NOT NULL REFERENCES sectors(id),
  straight_id INT NOT NULL REFERENCES sectors(id),
  deviation_id INT NOT NULL REFERENCES sectors(id),
  semaphore_id INT NOT NULL REFERENCES semaphores(id),
  is_working BOOL NOT NULL,
  is_deviating BOOL NOT NULL
);

CREATE TABLE IF NOT EXISTS Trains (
    id SERIAL PRIMARY KEY,
    train_number VARCHAR(10),
    length INT NOT NULL CHECK (length BETWEEN 6 AND 16),
    origin CHAR NOT NULL CHECK (origin IN ('L', 'R')),
    destination CHAR NOT NULL CHECK (destination IN ('L', 'R')),
    train_type CHAR NOT NULL CHECK (trainType IN ('R', 'V', 'F', 'S')),
    scheduled_arrival_time TIMESTAMP NOT NULL,
    scheduled_departure_time TIMESTAMP NOT NULL CHECK(scheduledDepartureTime > scheduledArrivalTime),
    is_transit BOOLEAN NOT NULL,
    assigned_platform INT REFERENCES sectors(platform_number)
);

CREATE TABLE IF NOT EXISTS TrainsLog (
    id SERIAL PRIMARY KEY,
    train_number VARCHAR(10),
    length INT NOT NULL CHECK (length BETWEEN 6 AND 16),
    origin CHAR NOT NULL CHECK (origin IN ('L', 'R')),
    destination CHAR NOT NULL CHECK (destination IN ('L', 'R')),
    train_type CHAR NOT NULL CHECK (trainType IN ('R', 'V', 'F', 'S')),
    scheduled_arrival_time TIMESTAMP NOT NULL,
    scheduled_departure_time TIMESTAMP NOT NULL CHECK(scheduledDepartureTime > scheduledArrivalTime),
    is_transit BOOLEAN NOT NULL,
    actual_departure_time TIMESTAMP NOT NULL,
    actual_arrival_time TIMESTAMP NOT NULL,
    assigned_platform INT REFERENCES sectors(platform_number)
);

CREATE TABLE IF NOT EXISTS Maintenance (
    id SERIAL PRIMARY KEY,
    train_d INT NOT NULL REFERENCES TrainsLog (id),
    maintenance_start TIMESTAMP NOT NULL,
    maintenance_end TIMESTAMP NOT NULL CHECK(maintenanceEnd > maintenanceStart),
    maintenance_platform INT REFERENCES sectors(id)
);
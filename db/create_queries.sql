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

CREATE TABLE IF NOT EXISTS trains (
    id SERIAL PRIMARY KEY,
    train_number VARCHAR(10),
    length INT NOT NULL CHECK (length BETWEEN 6 AND 16),
    origin CHAR(2) NOT NULL CHECK (origin IN ('L1', 'R1', 'L2', 'R2')),
    destination CHAR(1) NOT NULL CHECK (destination IN ('L', 'R')),
    train_type CHAR(1) NOT NULL CHECK (train_type IN ('R', 'V', 'F', 'S')),
    scheduled_arrival_time TIMESTAMP NOT NULL,
    scheduled_departure_time TIMESTAMP NOT NULL
        CHECK (scheduled_departure_time > scheduled_arrival_time),
    is_transit BOOLEAN NOT NULL,
    assigned_platform INT REFERENCES sectors(platform_number)
);


CREATE TABLE IF NOT EXISTS trains_log (
    id SERIAL PRIMARY KEY,
    train_number VARCHAR(10),
    length INT NOT NULL CHECK (length BETWEEN 6 AND 16),
    origin CHAR(2) NOT NULL CHECK (origin IN ('L1', 'R1', 'L2', 'R2')),
    destination CHAR(1) NOT NULL CHECK (destination IN ('L', 'R')),
    train_type CHAR(1) NOT NULL CHECK (train_type IN ('R', 'V', 'F', 'S')),
    scheduled_arrival_time TIMESTAMP NOT NULL,
    scheduled_departure_time TIMESTAMP NOT NULL
        CHECK (scheduled_departure_time > scheduled_arrival_time),
    is_transit BOOLEAN NOT NULL,
    actual_departure_time TIMESTAMP NOT NULL,
    actual_arrival_time TIMESTAMP NOT NULL,
    assigned_platform INT REFERENCES sectors(platform_number)
);

CREATE TABLE IF NOT EXISTS maintenance (
    id SERIAL PRIMARY KEY,
    train_id INT NOT NULL REFERENCES trains_log(id),
    maintenance_start TIMESTAMP NOT NULL,
    maintenance_end TIMESTAMP NOT NULL
        CHECK (maintenance_end > maintenance_start),
    maintenance_platform INT REFERENCES sectors(id)
);
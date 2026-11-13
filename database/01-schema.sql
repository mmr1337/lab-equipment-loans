CREATE TABLE equipment (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(40) NOT NULL UNIQUE,
    name varchar(120) NOT NULL
);

CREATE TABLE loans (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    equipment_id integer NOT NULL REFERENCES equipment (id),
    borrower varchar(120) NOT NULL,
    issued_at timestamptz NOT NULL DEFAULT now(),
    returned_at timestamptz
);

CREATE UNIQUE INDEX loans_one_active_per_equipment ON loans (equipment_id) WHERE returned_at IS NULL;

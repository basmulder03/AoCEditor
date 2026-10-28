CREATE TABLE answer_attempts (
    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    year INTEGER NOT NULL,
    day INTEGER NOT NULL,
    part INTEGER NOT NULL,
    answer TEXT NOT NULL,
    outcome TEXT NOT NULL,
    at INTEGER NOT NULL
);

CREATE INDEX answer_attempts_puzzle ON answer_attempts (year, day, part);

CREATE TABLE answer_cooldowns (
    year INTEGER NOT NULL,
    day INTEGER NOT NULL,
    part INTEGER NOT NULL,
    until INTEGER NOT NULL,
    PRIMARY KEY (year, day, part)
) WITHOUT ROWID;

CREATE TABLE known_answers (
    year INTEGER NOT NULL,
    day INTEGER NOT NULL,
    part INTEGER NOT NULL,
    answer TEXT NOT NULL,
    PRIMARY KEY (year, day, part)
) WITHOUT ROWID;

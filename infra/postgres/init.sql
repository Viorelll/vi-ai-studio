-- Runs once against the database created by POSTGRES_DB on first container
-- start. EF Core migrations own the schema; this only sets up extensions
-- the migrations rely on.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

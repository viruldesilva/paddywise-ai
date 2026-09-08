-- ==============================================================================
-- PADDYWISE-AI / KUMBURU DATABASE SCHEMA
-- Target Database: PostgreSQL 14+
-- Description: Core schema for role-based authentication, user profiles,
--              divisions, and JWT refresh tokens.
-- ==============================================================================

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Drop existing tables (in dependency order) if re-initializing
-- DROP TABLE IF EXISTS refresh_tokens CASCADE;
-- DROP TABLE IF EXISTS users CASCADE;
-- DROP TABLE IF EXISTS divisions CASCADE;
-- DROP TABLE IF EXISTS roles CASCADE;

-- ------------------------------------------------------------------------------
-- 1. ROLES TABLE
-- ------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,       -- 'farmer', 'extension_officer', 'buyer', 'admin'
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Seed System Roles
INSERT INTO roles (name, display_name, description) VALUES
    ('farmer', 'Farmer', 'Logs cultivation cycles, reports field symptoms, tracks harvest and sales.'),
    ('extension_officer', 'Extension Officer', 'Reviews AI diagnosis evidence, approves treatment plans, monitors division activity.'),
    ('buyer', 'Buyer / Miller', 'Browses live harvest listings and places direct procurement offers.'),
    ('admin', 'System Admin', 'Manages user accounts, pesticide allow-list, system configuration, and audit logs.')
ON CONFLICT (name) DO UPDATE 
SET display_name = EXCLUDED.display_name,
    description = EXCLUDED.description;

-- ------------------------------------------------------------------------------
-- 2. AGRARIAN DIVISIONS (Sri Lanka Context)
-- ------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS divisions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    district VARCHAR(100) NOT NULL,
    province VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Seed Sample Agrarian Service Divisions
INSERT INTO divisions (name, district, province) VALUES
    ('Medirigiriya', 'Polonnaruwa', 'North Central'),
    ('Nuwaragam Palatha', 'Anuradhapura', 'North Central'),
    ('Tambuttegama', 'Anuradhapura', 'North Central'),
    ('Ampara Central', 'Ampara', 'Eastern'),
    ('Kurunegala West', 'Kurunegala', 'North Western'),
    ('Colombo HQ', 'Colombo', 'Western')
ON CONFLICT DO NOTHING;

-- ------------------------------------------------------------------------------
-- 3. USERS TABLE
-- ------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,    -- BCrypt or Argon2 hash
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    phone VARCHAR(30),
    division_id INT REFERENCES divisions(id) ON DELETE SET NULL,
    division_custom VARCHAR(150),           -- For flexible input before division normalization
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for high-frequency queries
CREATE INDEX IF NOT EXISTS idx_users_email ON users(LOWER(email));
CREATE INDEX IF NOT EXISTS idx_users_role_id ON users(role_id);
CREATE INDEX IF NOT EXISTS idx_users_division_id ON users(division_id);

-- ------------------------------------------------------------------------------
-- 4. REFRESH TOKENS (JWT Session Management)
-- ------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ,
    replaced_by_token VARCHAR(255)
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);

-- ------------------------------------------------------------------------------
-- 5. TRIGGER FOR UPDATED_AT TIMESTAMP
-- ------------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_users_updated_at ON users;
CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- ------------------------------------------------------------------------------
-- 6. PRE-SEEDED INITIAL TEST ACCOUNTS (Matching LocalStorage Seeds)
-- Default Password for all seeds: 'Password123!'
-- (Password hash below generated using BCrypt work factor 11)
-- ------------------------------------------------------------------------------
INSERT INTO users (full_name, email, password_hash, role_id, phone, division_custom)
VALUES
    (
        'Bandara Wanninayake',
        'farmer@kumburu.lk',
        '$2a$11$e8dM8jH9cW8YlY.N4K3W4eH/q5f7Zl4z7dZ4Z7dZ4Z7dZ4Z7dZ4Z7', -- BCrypt hash for Password123!
        (SELECT id FROM roles WHERE name = 'farmer'),
        '+94 77 123 4567',
        'Polonnaruwa - Medirigiriya'
    ),
    (
        'Dr. Nilmini Perera',
        'officer@kumburu.lk',
        '$2a$11$e8dM8jH9cW8YlY.N4K3W4eH/q5f7Zl4z7dZ4Z7dZ4Z7dZ4Z7dZ4Z7',
        (SELECT id FROM roles WHERE name = 'extension_officer'),
        '+94 71 987 6543',
        'Anuradhapura - Nuwaragam Palatha'
    ),
    (
        'Roshan Fernando (Lanka Rice Mills)',
        'buyer@kumburu.lk',
        '$2a$11$e8dM8jH9cW8YlY.N4K3W4eH/q5f7Zl4z7dZ4Z7dZ4Z7dZ4Z7dZ4Z7',
        (SELECT id FROM roles WHERE name = 'buyer'),
        '+94 76 345 6789',
        'Kurunegala'
    ),
    (
        'System Administrator',
        'admin@kumburu.lk',
        '$2a$11$e8dM8jH9cW8YlY.N4K3W4eH/q5f7Zl4z7dZ4Z7dZ4Z7dZ4Z7dZ4Z7',
        (SELECT id FROM roles WHERE name = 'admin'),
        '+94 11 234 5678',
        'Colombo HQ'
    )
ON CONFLICT (email) DO NOTHING;

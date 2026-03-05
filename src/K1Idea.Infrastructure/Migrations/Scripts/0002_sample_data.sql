-- ─────────────────────────────────────────────────────────────
-- 0002_sample_data.sql
-- Idempotent seed data for local development / demo environments.
-- Safe to re-run: all inserts use ON CONFLICT DO NOTHING.
-- ─────────────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────
-- Fixed UUIDs
-- ─────────────────────────────────────────────────────────────
-- tenant_id            : a0000000-0000-0000-0000-000000000001
-- org_id               : b0000000-0000-0000-0000-000000000001
-- bu_frontend_id       : c0000000-0000-0000-0000-000000000001
-- bu_backend_id        : c0000000-0000-0000-0000-000000000002
-- user_id              : d0000000-0000-0000-0000-000000000001
-- ticket_idea_id       : e0000000-0000-0000-0000-000000000001
-- ticket_initiative_id : e0000000-0000-0000-0000-000000000002
-- ticket_project_id    : e0000000-0000-0000-0000-000000000003
-- ticket_task_id       : e0000000-0000-0000-0000-000000000004

-- ─────────────────────────────────────────────────────────────
-- Tenant
-- ─────────────────────────────────────────────────────────────
INSERT INTO tenants (id, slug, name, created_at)
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'acme',
    'Acme Corp',
    NOW()
)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- Organization
-- ─────────────────────────────────────────────────────────────
INSERT INTO organizations (id, tenant_id, slug, name, created_at)
VALUES (
    'b0000000-0000-0000-0000-000000000001',
    'a0000000-0000-0000-0000-000000000001',
    'eng',
    'Engineering',
    NOW()
)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- Business Units
-- ─────────────────────────────────────────────────────────────
INSERT INTO business_units (id, tenant_id, org_id, slug, name, created_at)
VALUES
    (
        'c0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'frontend',
        'Frontend',
        NOW()
    ),
    (
        'c0000000-0000-0000-0000-000000000002',
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'backend',
        'Backend',
        NOW()
    )
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- User
-- password_hash is BCrypt cost 12 of 'password123'
-- ─────────────────────────────────────────────────────────────
INSERT INTO users (id, email, display_name, password_hash, created_at, updated_at)
VALUES (
    'd0000000-0000-0000-0000-000000000001',
    'admin@acme.com',
    'Admin User',
    chr(36) || '2a' || chr(36) || '12' || chr(36) || '1JlDf.DQw6EHJhLBaptdvOS6ePLrDHA7s/8TAWWmBYxmC934hG9Je',
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- Org membership
-- ─────────────────────────────────────────────────────────────
INSERT INTO org_users (id, tenant_id, org_id, user_id, role, created_at)
VALUES (
    gen_random_uuid(),
    'a0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'Admin',
    NOW()
)
ON CONFLICT (org_id, user_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- Business unit memberships
-- ─────────────────────────────────────────────────────────────
INSERT INTO business_unit_users (id, tenant_id, org_id, business_unit_id, user_id, created_at)
VALUES
    (
        gen_random_uuid(),
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000001',  -- Frontend
        'd0000000-0000-0000-0000-000000000001',
        NOW()
    ),
    (
        gen_random_uuid(),
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000002',  -- Backend
        'd0000000-0000-0000-0000-000000000001',
        NOW()
    )
ON CONFLICT (business_unit_id, user_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- Tickets
-- Inserted in hierarchy order: Idea → Initiative → Project → Task
-- The enforce_ticket_hierarchy trigger validates parent_id rules.
-- ─────────────────────────────────────────────────────────────

-- Idea (no parent)
INSERT INTO tickets (
    id, tenant_id, org_id, owner_business_unit_id,
    title, description,
    reporter_id, assignee_id,
    type, status, priority,
    parent_id, created_at, updated_at
)
VALUES (
    'e0000000-0000-0000-0000-000000000001',
    'a0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000002',  -- owned by Backend BU
    'Improve developer experience',
    'Identify and address pain points in the day-to-day developer workflow.',
    'd0000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'Idea',
    'Backlog',
    'High',
    NULL,
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- Initiative (parent = Idea)
INSERT INTO tickets (
    id, tenant_id, org_id, owner_business_unit_id,
    title, description,
    reporter_id, assignee_id,
    type, status, priority,
    parent_id, created_at, updated_at
)
VALUES (
    'e0000000-0000-0000-0000-000000000002',
    'a0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000002',  -- owned by Backend BU
    'Streamline CI/CD pipeline',
    'Reduce build and deployment times by modernising the CI/CD pipeline.',
    'd0000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'Initiative',
    'InProgress',
    'High',
    'e0000000-0000-0000-0000-000000000001',
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- Project (parent = Initiative)
INSERT INTO tickets (
    id, tenant_id, org_id, owner_business_unit_id,
    title, description,
    reporter_id, assignee_id,
    type, status, priority,
    parent_id, created_at, updated_at
)
VALUES (
    'e0000000-0000-0000-0000-000000000003',
    'a0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000002',  -- owned by Backend BU
    'Add GitHub Actions workflow',
    'Introduce a GitHub Actions CI/CD workflow to replace the existing solution.',
    'd0000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'Project',
    'InProgress',
    'Medium',
    'e0000000-0000-0000-0000-000000000002',
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- Task (parent = Project)
INSERT INTO tickets (
    id, tenant_id, org_id, owner_business_unit_id,
    title, description,
    reporter_id, assignee_id,
    type, status, priority,
    parent_id, created_at, updated_at
)
VALUES (
    'e0000000-0000-0000-0000-000000000004',
    'a0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000002',  -- owned by Backend BU
    'Write unit test pipeline step',
    'Add a dedicated pipeline step that runs the full xUnit test suite with coverage collection.',
    'd0000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'Task',
    'Backlog',
    'Medium',
    'e0000000-0000-0000-0000-000000000003',
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────
-- Share all tickets to the Frontend business unit
-- (tickets are owned by Backend BU; sharing extends visibility)
-- ─────────────────────────────────────────────────────────────
INSERT INTO ticket_business_units (id, tenant_id, org_id, ticket_id, business_unit_id, created_at)
VALUES
    (
        gen_random_uuid(),
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'e0000000-0000-0000-0000-000000000001',  -- Idea
        'c0000000-0000-0000-0000-000000000001',  -- Frontend BU
        NOW()
    ),
    (
        gen_random_uuid(),
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'e0000000-0000-0000-0000-000000000002',  -- Initiative
        'c0000000-0000-0000-0000-000000000001',  -- Frontend BU
        NOW()
    ),
    (
        gen_random_uuid(),
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'e0000000-0000-0000-0000-000000000003',  -- Project
        'c0000000-0000-0000-0000-000000000001',  -- Frontend BU
        NOW()
    ),
    (
        gen_random_uuid(),
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'e0000000-0000-0000-0000-000000000004',  -- Task
        'c0000000-0000-0000-0000-000000000001',  -- Frontend BU
        NOW()
    )
ON CONFLICT (ticket_id, business_unit_id) DO NOTHING;
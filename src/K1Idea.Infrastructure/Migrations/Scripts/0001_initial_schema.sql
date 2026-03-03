CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ─────────────────────────────────────────────────────────────
-- Tenancy
-- ─────────────────────────────────────────────────────────────
CREATE TABLE tenants (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug         TEXT NOT NULL UNIQUE,
  name         TEXT NOT NULL,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE organizations (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id    UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  slug         TEXT NOT NULL,
  name         TEXT NOT NULL,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, slug)
);

CREATE TABLE business_units (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id    UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  org_id       UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  slug         TEXT NOT NULL,
  name         TEXT NOT NULL,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (org_id, slug)
);

-- ─────────────────────────────────────────────────────────────
-- Users & memberships
-- ─────────────────────────────────────────────────────────────
CREATE TABLE users (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email         TEXT NOT NULL UNIQUE,
  display_name  TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE org_users (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id  UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  org_id     UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  role       TEXT NOT NULL CHECK (role IN ('Admin','Member','Viewer')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (org_id, user_id)
);

CREATE TABLE business_unit_users (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id        UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  org_id           UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  business_unit_id UUID NOT NULL REFERENCES business_units(id) ON DELETE CASCADE,
  user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (business_unit_id, user_id)
);

CREATE TABLE refresh_tokens (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token       TEXT NOT NULL UNIQUE,
  tenant_id   UUID,
  org_id      UUID,
  expires_at  TIMESTAMPTZ NOT NULL,
  revoked_at  TIMESTAMPTZ,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ─────────────────────────────────────────────────────────────
-- Tickets (Idea/Initiative/Project/Task)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE tickets (
  id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id              UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  org_id                 UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  owner_business_unit_id UUID NOT NULL REFERENCES business_units(id),

  title       TEXT NOT NULL,
  description TEXT,

  reporter_id UUID NOT NULL REFERENCES users(id),
  assignee_id UUID REFERENCES users(id),

  type     TEXT NOT NULL CHECK (type IN ('Idea','Initiative','Project','Task')),
  status   TEXT NOT NULL DEFAULT 'Backlog'
           CHECK (status IN ('Backlog','InProgress','InReview','Done','Cancelled')),
  priority TEXT NOT NULL DEFAULT 'Medium'
           CHECK (priority IN ('Low','Medium','High','Critical')),

  parent_id  UUID REFERENCES tickets(id),

  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

-- Sharing
CREATE TABLE ticket_business_units (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id        UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  org_id           UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  ticket_id        UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
  business_unit_id UUID NOT NULL REFERENCES business_units(id) ON DELETE CASCADE,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (ticket_id, business_unit_id)
);

-- Comments
CREATE TABLE comments (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id  UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  org_id     UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  ticket_id  UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
  author_id  UUID NOT NULL REFERENCES users(id),
  body       TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ─────────────────────────────────────────────────────────────
-- Hierarchy enforcement trigger
-- ─────────────────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION enforce_ticket_hierarchy()
RETURNS TRIGGER AS $$
DECLARE parent_type TEXT;
DECLARE parent_tenant UUID;
DECLARE parent_org UUID;
BEGIN
  IF NEW.parent_id IS NULL THEN
    IF NEW.type = 'Idea' THEN
      RETURN NEW;
    END IF;
    RAISE EXCEPTION 'Non-Idea items must have a parent';
  END IF;

  SELECT type, tenant_id, org_id
    INTO parent_type, parent_tenant, parent_org
    FROM tickets
    WHERE id = NEW.parent_id;

  IF parent_tenant IS NULL THEN
    RAISE EXCEPTION 'Parent ticket not found';
  END IF;

  IF parent_tenant <> NEW.tenant_id OR parent_org <> NEW.org_id THEN
    RAISE EXCEPTION 'Parent and child must be within same tenant and org';
  END IF;

  IF (NEW.type = 'Idea') THEN
    RAISE EXCEPTION 'Idea cannot have a parent';
  ELSIF (NEW.type = 'Initiative' AND parent_type <> 'Idea') THEN
    RAISE EXCEPTION 'Initiative must be child of Idea';
  ELSIF (NEW.type = 'Project' AND parent_type <> 'Initiative') THEN
    RAISE EXCEPTION 'Project must be child of Initiative';
  ELSIF (NEW.type = 'Task' AND parent_type <> 'Project') THEN
    RAISE EXCEPTION 'Task must be child of Project';
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER ticket_hierarchy_check
BEFORE INSERT OR UPDATE ON tickets
FOR EACH ROW EXECUTE FUNCTION enforce_ticket_hierarchy();

-- ─────────────────────────────────────────────────────────────
-- Indexes
-- ─────────────────────────────────────────────────────────────
CREATE INDEX idx_org_users_org      ON org_users(org_id);
CREATE INDEX idx_bu_users_user      ON business_unit_users(user_id);
CREATE INDEX idx_tickets_scope      ON tickets(tenant_id, org_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_tickets_owner_bu   ON tickets(owner_business_unit_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_ticket_bu_ticket   ON ticket_business_units(ticket_id);
CREATE INDEX idx_ticket_bu_bu       ON ticket_business_units(business_unit_id);
CREATE INDEX idx_comments_ticket    ON comments(ticket_id);

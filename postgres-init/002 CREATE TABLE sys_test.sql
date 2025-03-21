CREATE TABLE sys_test (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    test_amount INT NOT NULL,
    answers_json JSONB NOT NULL,
    creator_user_id BIGINT NOT NULL,
    creator_user_full_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW()
);
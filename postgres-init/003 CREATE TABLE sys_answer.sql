CREATE TABLE sys_answer (
    id SERIAL PRIMARY KEY,
    test_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    answers_json JSONB NOT NULL,
    correct_count INT NOT NULL,
    incorrect_count INT NOT NULL,
    percentage FLOAT NOT NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW()
);

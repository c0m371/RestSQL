DROP TABLE if exists public.users;

CREATE TABLE public.users (
	username varchar NOT NULL,
	first_name varchar NOT NULL,
	last_name varchar NOT NULL,
	CONSTRAINT user_pk PRIMARY KEY (username)
);

DROP TABLE if exists public.posts;

-- Note: FK to users missing
CREATE TABLE public.posts (
	id int4 GENERATED ALWAYS AS IDENTITY NOT NULL,
	username varchar not null,
	title varchar NOT NULL,
	description varchar NULL,
	creation_date date NOT NULL,
	CONSTRAINT post_pk PRIMARY KEY (id)
);

DROP TABLE if exists public.tags;

-- Note: FK to posts missing
CREATE TABLE public.tags (
	post_id int4 NOT NULL,
	tag varchar NOT NULL,
	CONSTRAINT tags_pk PRIMARY KEY (post_id, tag)
);

-- Insert 2 users
INSERT INTO public.users (username, first_name, last_name) VALUES
('alice_codes', 'Alice', 'Smith'),
('bob_devs', 'Robert', 'Jones');

-- Insert 4 posts (ID is GENERATED ALWAYS AS IDENTITY)
-- Post 1 (Author: alice_codes)
INSERT INTO public.posts (title, description, creation_date, username) VALUES
('The Joys of Async/Await', 'A deep dive into non-blocking operations in C#.', CURRENT_DATE, 'alice_codes');

-- Post 2 (Author: alice_codes)
INSERT INTO public.posts (title, description, creation_date, username) VALUES
('PostgreSQL vs MySQL: A Performance Review', 'Comparing the speed and features of two popular databases.', CURRENT_DATE - INTERVAL '1 day', 'alice_codes');

-- Post 3 (Author: bob_devs)
INSERT INTO public.posts (title, description, creation_date, username) VALUES
('Understanding the SOLID Principles', 'A beginner''s guide to maintaining clean, scalable code.', CURRENT_DATE - INTERVAL '7 days', 'bob_devs');

-- Post 4 (Author: bob_devs)
INSERT INTO public.posts (title, description, creation_date, username) VALUES
('Advanced Dapper Mappings', 'How to handle complex type projections and multi-mapping with Dapper.', CURRENT_DATE - INTERVAL '3 days', 'bob_devs');

-- Insert 6 tags
INSERT INTO public.tags (post_id, tag) VALUES
-- Tags for Post 1 (The Joys of Async/Await)
(1, 'C#'),
(1, 'Async'),

-- Tags for Post 2 (PostgreSQL vs MySQL)
(2, 'PostgreSQL'),
(2, 'Database'),

-- Tags for Post 3 (Understanding the SOLID Principles)
(3, 'OOP'),

-- Tags for Post 4 (Advanced Dapper Mappings)
(4, 'Dapper');
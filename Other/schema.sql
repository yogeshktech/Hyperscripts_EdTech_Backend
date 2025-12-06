CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    category_name VARCHAR(255) NOT NULL,
    category_discription TEXT,
    category_slug VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    category_image TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


CREATE TABLE subcategories (
    id SERIAL PRIMARY KEY,
    category_id INT NOT NULL,
    sub_category_name VARCHAR(255) NOT NULL,
    sub_category_slug VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    sub_category_image VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_category
        FOREIGN KEY(category_id)
        REFERENCES categories(id)
        ON DELETE CASCADE
);


CREATE TABLE courses (
    id SERIAL PRIMARY KEY,
    course_name VARCHAR(255) NOT NULL,
    course_discription TEXT,
    course_slug VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    course_image TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    category_id INT,
    start_class_date TIMESTAMP,
    maximum_lpa VARCHAR(255),
    minimum_lpa VARCHAR(255),
    demo_start_date TIMESTAMP,
    demo_end_date TIMESTAMP,
    mrp_price ,
    saling_price,
    course_level VARCHAR(255),
    duration VARCHAR(255),
    total_lectures VARCHAR(255),
    course_language VARCHAR(255),
    overview TEXT,
    course_highlights TEXT,
    course_details TEXT,
    why_choose_us TEXT,
    Progress INT

);
ALTER TABLE courses
ALTER COLUMN course_language TYPE INTEGER USING NULL;

CREATE TABLE languages (
    id SERIAL PRIMARY KEY,
    language_name VARCHAR(255) NOT NULL,
    language_discription TEXT,
    language_slug VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


CREATE TABLE blogs (
    id SERIAL PRIMARY KEY,
    blogs_name VARCHAR(255) NOT NULL,
    blogs_discription TEXT,
    blogs_slug VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE blogs
ADD COLUMN blogs_image VARCHAR(255);

CREATE TABLE testimonial (
    id SERIAL PRIMARY KEY,
    test_name VARCHAR(255) NOT NULL,
    discription TEXT,
    test_content TEXT,
    slug VARCHAR(255),
    image VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


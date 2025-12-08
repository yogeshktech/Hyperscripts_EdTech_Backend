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
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    category_id INT,
    start_class_date TIMESTAMP,
    maximum_lpa VARCHAR(255),
    minimum_lpa VARCHAR(255),
    demo_start_date TIMESTAMP,
    demo_end_date TIMESTAMP,
    mrp_price NUMERIC,
    saling_price NUMERIC,
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

<<<<<<< HEAD
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

CREATE TABLE reviews (
    id SERIAL PRIMARY KEY,
    
    user_id TEXT NOT NULL,
    course_id INT NOT NULL,
    
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(255),
    review_text TEXT,

    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_review_user
        FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,

    CONSTRAINT fk_review_course
        FOREIGN KEY (course_id) REFERENCES courses(id) ON DELETE CASCADE
=======


CREATE TABLE faculties (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    course_id INT,
    position VARCHAR(255),
    experience VARCHAR(255),
    specialization VARCHAR(255),
    profile_image TEXT,
    status BOOLEAN DEFAULT TRUE,
    created_by VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE enquires (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    contact VARCHAR(255),
    message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
>>>>>>> 82b384e5b60979096a6d1f495cbe03e327b99ce7
);
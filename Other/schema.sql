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



CREATE TABLE faculties (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    course_id INT(50) USING NULL,
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
);

CREATE TABLE cart_items (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price NUMERIC(10,2) NOT NULL,
    discount NUMERIC(10,2) DEFAULT 0,

    total NUMERIC(10,2) GENERATED ALWAYS AS (discount * quantity) STORED,

    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

ALTER TABLE cart_items 
ADD COLUMN ip_address VARCHAR(50);

ALTER TABLE cart_items 
ALTER COLUMN user_id DROP NOT NULL;



ALTER TABLE "AspNetUsers"
ADD COLUMN IF NOT EXISTS slug TEXT,
ADD COLUMN IF NOT EXISTS position TEXT,
ADD COLUMN IF NOT EXISTS experience TEXT,
ADD COLUMN IF NOT EXISTS specialization TEXT,
ADD COLUMN IF NOT EXISTS profile_image TEXT,
ADD COLUMN IF NOT EXISTS status BOOLEAN DEFAULT TRUE,
ADD COLUMN IF NOT EXISTS created_by TEXT,
ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP DEFAULT NOW();



CREATE TABLE enquiries
(
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    contact VARCHAR(20),
    message TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW()
);

CREATE TABLE carrers
(
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    contact VARCHAR(20),
    subject VARCHAR(255),
    resume VARCHAR(500),   -- file path or file name
    message TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW()
);


CREATE TABLE blog_comments (
    id SERIAL PRIMARY KEY,

    blog_id INT NOT NULL,
    user_id TEXT NOT NULL,  -- AspNetUsers.Id is TEXT (UUID/string)

    parent_comment_id INT NULL,

    comment TEXT NOT NULL,

    is_deleted BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL,

    CONSTRAINT fk_blog
        FOREIGN KEY (blog_id)
        REFERENCES blogs(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user
        FOREIGN KEY (user_id)
        REFERENCES "AspNetUsers"("Id")
        ON DELETE CASCADE,

    CONSTRAINT fk_parent_comment
        FOREIGN KEY (parent_comment_id)
        REFERENCES blog_comments(id)
        ON DELETE CASCADE
);


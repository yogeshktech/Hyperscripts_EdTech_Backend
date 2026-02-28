select * from "AspNetUsers" anu ;

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

select * from subcategories;


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

    mrp_price NUMERIC(10,2),
    saling_price NUMERIC(10,2),

    course_level VARCHAR(255),
    duration VARCHAR(255),
    total_lectures VARCHAR(255),
    course_language VARCHAR(255),

    overview TEXT,
    course_highlights TEXT,
    course_details TEXT,
    why_choose_us TEXT,

    progress NUMERIC(10,2)
);

ALTER TABLE courses
ALTER COLUMN coupon_id TYPE INTEGER USING NULL;

CREATE TABLE languages (
    id SERIAL PRIMARY KEY,
    language_name VARCHAR(255) NOT NULL,
    language_discription TEXT,
    language_slug VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

select * from languages ;

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

select * from testimonial ;

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
);


select * from reviews ;

select * from "AspNetUsers" anu ;


CREATE TABLE cart_items (
    id SERIAL PRIMARY KEY,
    user_id UUID NULL,
    course_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price NUMERIC(10,2) NOT NULL,
    discount NUMERIC(10,2) DEFAULT 0,

    total NUMERIC(10,2) GENERATED ALWAYS AS (discount * quantity) STORED,

    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    ip_address TEXT
);

ALTER TABLE cart_items 
ADD COLUMN saling_price NUMERIC(10,2) NOT NULL;

ALTER TABLE cart_items 
ALTER COLUMN user_id DROP NOT NULL;

CREATE TABLE coupons (
    id SERIAL PRIMARY KEY,

    -- Coupon code like "WELCOME50"
    code VARCHAR(50) NOT NULL UNIQUE,

    -- Type: FLAT = fixed discount, PERCENT = percentage discount
    discount_type VARCHAR(20) NOT NULL CHECK (discount_type IN ('FLAT', 'PERCENT')),

    -- Discount value (flat amount or percentage)
    discount_value NUMERIC(10,2) NOT NULL,

    -- Minimum order amount required to apply coupon
    min_order_value NUMERIC(10,2) DEFAULT 0,

    -- Maximum discount allowed (for percentage coupons)
    max_discount NUMERIC(10,2),

    -- Optional course restriction (NULL means applicable for all courses)
    course_id INT NULL REFERENCES courses(id) ON DELETE SET NULL,

    -- How many times a user can use this coupon
    usage_limit_per_user INT DEFAULT 1,

    -- Total usage limit for this coupon across all users
    total_usage_limit INT DEFAULT 1000,

    -- Validity dates
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,

    -- Is the coupon active?
    is_active BOOLEAN DEFAULT TRUE,

    -- Created and updated timestamps
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL,                   -- GUID user_id
    coupon_id INT REFERENCES coupons(id),            -- Nullable (if no coupon)
    subtotal NUMERIC(10,2) NOT NULL,                 -- Before discount
    discount_amount NUMERIC(10,2) DEFAULT 0,         -- Calculated after coupon
    total_amount NUMERIC(10,2) NOT NULL,             -- Final payable after coupon
    payment_status VARCHAR(20) DEFAULT 'pending',    -- pending/paid/failed
    order_status VARCHAR(20) DEFAULT 'pending',      -- pending/processing/shipped
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

select * from orders;
select * from order_items;


CREATE TABLE order_items (
    id SERIAL PRIMARY KEY,
    order_id INT REFERENCES orders(id) ON DELETE CASCADE,
    course_id INT NOT NULL,
    price NUMERIC(10,2) NOT NULL,         -- Original price
    discount NUMERIC(10,2) DEFAULT 0,     -- Discount per course (if any)
    quantity INT DEFAULT 1,
    total NUMERIC(10,2) GENERATED ALWAYS AS ((price - discount) * quantity) STORED
);

CREATE TABLE faculties (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    course_id INTEGER,
    position VARCHAR(255),
    experience VARCHAR(255),
    specialization VARCHAR(255),
    profile_image TEXT,
    status BOOLEAN DEFAULT TRUE,
    created_by VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

select * from faculties;


ALTER TABLE courses
ADD COLUMN coupon_id INTEGER;

ALTER TABLE orders
ADD COLUMN razorpay_order_id VARCHAR(100),
ADD COLUMN razorpay_payment_id VARCHAR(100),
ADD COLUMN razorpay_signature TEXT;



select * from courses;
select * from orders;
select * from order_items;
select * from batches b ;
select * from batch_faculties bf ;
select * from user_courses c ;
select * from live_classes lc ;
select * from live_class_attendance lca  ;

select * from "AspNetUsers" anu ;
select * from faculties f ;
select * from "AspNetUserRoles" anur ;
select * from "AspNetRoles" anr ;

CREATE TABLE user_courses (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id INT NOT NULL,
    order_id INT REFERENCES orders(id) ON DELETE CASCADE,

    access_type VARCHAR(20) DEFAULT 'FULL',   -- FULL / TRIAL
    is_active BOOLEAN DEFAULT TRUE,

    progress_percent INT DEFAULT 0,
    completed_at TIMESTAMP,

    valid_till DATE, -- NULL = lifetime access

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    UNIQUE(user_id, course_id)
);


CREATE TABLE course_progress (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id INT NOT NULL,
    lecture_id INT NOT NULL,
    watched_seconds INT DEFAULT 0,
    is_completed BOOLEAN DEFAULT FALSE,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, lecture_id)
);


CREATE TABLE certificates (
    id SERIAL PRIMARY KEY,
    user_id UUID,
    course_id INT,
    certificate_url TEXT,
    issued_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


INSERT INTO user_courses
(
    user_id,
    course_id,
    order_id,
    access_type,
    is_active,
    progress_percent,
    valid_till
)
VALUES
-- 1️⃣ Course 101
(
    'b5661888-a381-4292-965b-9623c389db10',
    101,
    1,
    'FULL',
    TRUE,
    0,
    NULL
),
-- 2️⃣ Course 102
(
    'b353f7f8-a9cb-4e5c-85d7-bd8a50c85879',
    102,
    1,
    'FULL',
    TRUE,
    0,
    NULL
),
-- 3️⃣ Course 103
(
    '61829959-94fc-40dc-a9fd-847f949ac955',
    103,
    1,
    'FULL',
    TRUE,
    0,
    NULL
),
-- 4️⃣ Course 104
(
    'c24c6d28-abd8-4cd5-b77d-50f000aef774',
    104,
    1,
    'FULL',
    TRUE,
    0,
    NULL
),
-- 5️⃣ Course 105
(
    'ccc65b7a-a252-4a61-9c31-0d2e10a84db0',
    105,
    1,
    'FULL',
    TRUE,
    0,
    NULL
);

UPDATE user_courses
SET
    course_id = 51




select * from user_courses;
select * from courses;

CREATE TABLE batches (
    id SERIAL PRIMARY KEY,
    course_id INT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,

    batch_name VARCHAR(100),          -- e.g. "Morning Batch"
    start_date DATE NOT NULL,
    end_date DATE,

    start_time TIME,
    end_time TIME,

    max_students INT ,
    is_active BOOLEAN DEFAULT TRUE,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE user_batches (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL,
    course_id INT NOT NULL,
    batch_id INT NOT NULL REFERENCES batches(id),

    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,

    UNIQUE(user_id, batch_id)
);


CREATE TABLE live_classes (
    id SERIAL PRIMARY KEY,
    batch_id INT REFERENCES batches(id) ON DELETE CASCADE,

    topic VARCHAR(200),
    class_date DATE,
    start_time TIME,
    end_time TIME,

    meeting_link TEXT,          -- Zoom / Meet
    recording_link TEXT,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE live_class_attendance (
    id SERIAL PRIMARY KEY,
    live_class_id INT REFERENCES live_classes(id),
    user_id UUID,

    joined_at TIMESTAMP,
    left_at TIMESTAMP,
    attended BOOLEAN DEFAULT FALSE
);

select * from live_classes lc ;
select * from live_class_attendance lca ;

INSERT INTO user_batches (user_id, course_id, batch_id, joined_at, is_active)
VALUES 
('d4888721-66b2-40a3-8a47-d6f51b3481f2', 61, 3, CURRENT_TIMESTAMP, TRUE);


select * from batches b ;
select * from user_batches ub ;
select * from live_classes lc ;
select * from live_class_attendance lca ;
select * from courses;
select * from cart_items ci ;


ALTER TABLE live_classes
ADD COLUMN recording_status VARCHAR(20) DEFAULT 'pending';
 

select * from "AspNetUsers" anu ;

select * from courses;
select * from orders;
select * from order_items;
select * from cart_items;
select * from coupons c ;

SELECT *
FROM orders
WHERE order_status = 'CONFIRMED'
  AND payment_status = 'PAID'
ORDER BY id DESC;

ALTER TABLE orders
ALTER COLUMN user_id TYPE UUID
USING user_id::uuid;

SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'AspNetUsers';


select * from "AspNetUsers" anu ;
select * from faculties f ;
select * from "AspNetUserRoles" anur ;
select * from "AspNetRoles" anr ;

TRUNCATE TABLE
    order_items,
    user_courses,
    orders
RESTART IDENTITY;


CREATE TABLE batch_faculties (
    id SERIAL PRIMARY KEY,
    batch_id INT NOT NULL REFERENCES batches(id) ON DELETE CASCADE,
    faculties_id UUID NOT NULL,  -- from users table

    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,

    UNIQUE (batch_id, faculties_id)
);

ALTER TABLE batch_faculties
ADD CONSTRAINT uq_batch_faculty UNIQUE (batch_id, faculties_id);


select * from batch_faculties;

select * from coupons;

SELECT
    o.id AS order_id,
    o.subtotal,
    o.discount_amount,
    o.total_amount,
    o.payment_status,
    o.order_status,

    u."Id"          AS user_id,
    u."UserName"    AS name,
    u."Email"       AS email,
    u."PhoneNumber" AS mobile
FROM orders o
INNER JOIN "AspNetUsers" u
    ON u."Id" = o.user_id
WHERE o.id = 8;

ALTER TABLE "AspNetUsers"
ADD COLUMN IF NOT EXISTS "Gender" VARCHAR(10),
ADD COLUMN IF NOT EXISTS "Address" TEXT,
ADD COLUMN IF NOT EXISTS "DateOfBirth" DATE,
ADD COLUMN IF NOT EXISTS "Subject" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "ExperienceYears" INT,
ADD COLUMN IF NOT EXISTS "Salary" NUMERIC(10,2),
ADD COLUMN IF NOT EXISTS "Position" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "Specialization" VARCHAR(50),
ADD COLUMN IF NOT EXISTS "ImagePath" TEXT;

ALTER TABLE "live_classes"
DROP COLUMN "faculty_id";


select * from faculties f ;
select * from "AspNetUsers" anu ;

select * from live_classes lc ;
select * from batches b ;
select * from batch_faculties bf ;

ALTER TABLE live_classes
ADD COLUMN IF NOT EXISTS faculty_id TEXT;

truncate table live_classes

SELECT
    lc.id,
    lc.batch_id,
    lc.topic,
    lc.class_date,
    lc.start_time,
    lc.end_time,
    lc.meeting_link,
    lc.recording_link,
    lc.image_path AS image,
    lc.created_at,

    anu."Id"           AS user_id,
    anu."FirstName",
    anu."LastName",
    anu."Email",
    anu."PhoneNumber",
    anu."UserName"
FROM live_classes lc
LEFT JOIN "AspNetUsers" anu
    ON anu."Id"::UUID = lc.faculty_id
WHERE lc.status = true
ORDER BY lc.class_date DESC, lc.start_time ASC;


select * from reviews r 

select * from "AspNetUsers" anu where "Id" = 'cd863c2b-14d5-46aa-9b4a-2001df3d3e62';

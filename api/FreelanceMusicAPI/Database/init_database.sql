-- Freelance Music Database Initialization Script
-- This script creates a clean SQLite database with proper table structure

-- Enable foreign key constraints
PRAGMA foreign_keys = ON;

-- Drop existing tables if they exist (for clean setup)
DROP TABLE IF EXISTS Student_Studying;
DROP TABLE IF EXISTS Teacher_Day_Availability;
DROP TABLE IF EXISTS Teacher;
DROP TABLE IF EXISTS Student;

-- Create Student Table
CREATE TABLE Student (
    student_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_name TEXT NOT NULL,
    student_email TEXT NOT NULL UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    studentpassword TEXT
);

-- Create Teacher Table
CREATE TABLE Teacher (
    teacher_id INTEGER PRIMARY KEY AUTOINCREMENT,
    teacher_name TEXT NOT NULL,
    teacher_email TEXT NOT NULL UNIQUE,
    instrument TEXT NOT NULL,
    class_full INTEGER DEFAULT 0 CHECK (class_full IN (0, 1)), -- 0 = false, 1 = true
    class_limit INTEGER DEFAULT 10,
    current_class INTEGER DEFAULT 0,
    bio TEXT,
    teacher_password TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Create Student_Studying Table (Junction table for many-to-many relationship)
CREATE TABLE Student_Studying (
    student_id INTEGER NOT NULL,
    teacher_id INTEGER NOT NULL,
    day TEXT NOT NULL CHECK (day IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (student_id, teacher_id, day),
    FOREIGN KEY (student_id) REFERENCES Student(student_id) ON DELETE CASCADE,
    FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
);

-- Create Teacher_Day_Availability Table
CREATE TABLE Teacher_Day_Availability (
    teacher_id INTEGER NOT NULL,
    day TEXT NOT NULL CHECK (day IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')),
    available INTEGER DEFAULT 1 CHECK (available IN (0, 1)), -- 0 = not available, 1 = available
    start_time TIME,
    end_time TIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (teacher_id, day),
    FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX idx_student_email ON Student(student_email);
CREATE INDEX idx_teacher_email ON Teacher(teacher_email);
CREATE INDEX idx_teacher_instrument ON Teacher(instrument);
CREATE INDEX idx_student_studying_student ON Student_Studying(student_id);
CREATE INDEX idx_student_studying_teacher ON Student_Studying(teacher_id);
CREATE INDEX idx_teacher_availability_teacher ON Teacher_Day_Availability(teacher_id);
CREATE INDEX idx_teacher_availability_day ON Teacher_Day_Availability(day);

-- Insert sample data for testing
INSERT INTO Student (student_name, student_email) VALUES 
('John Smith', 'john.smith@email.com'),
('Sarah Johnson', 'sarah.johnson@email.com'),
('Mike Davis', 'mike.davis@email.com');

INSERT INTO Teacher (teacher_name, teacher_email, instrument, class_full, class_limit, current_class, bio) VALUES 
('Alice Wilson', 'alice.wilson@email.com', 'Piano', 0, 8, 0, 'Professional pianist with 15 years of teaching experience'),
('Bob Brown', 'bob.brown@email.com', 'Guitar', 0, 10, 0, 'Classical and acoustic guitar specialist'),
('Carol Green', 'carol.green@email.com', 'Violin', 1, 6, 6, 'Orchestral violinist and music theory expert'),
('David Lee', 'david.lee@email.com', 'Drums', 0, 12, 0, 'Jazz and rock drumming instructor');

INSERT INTO Teacher_Day_Availability (teacher_id, day, available, start_time, end_time) VALUES 
(1, 'Monday', 1, '09:00', '17:00'),
(1, 'Tuesday', 1, '09:00', '17:00'),
(1, 'Wednesday', 1, '09:00', '17:00'),
(1, 'Thursday', 1, '09:00', '17:00'),
(1, 'Friday', 1, '09:00', '15:00'),
(2, 'Monday', 1, '10:00', '18:00'),
(2, 'Tuesday', 1, '10:00', '18:00'),
(2, 'Wednesday', 1, '10:00', '18:00'),
(2, 'Thursday', 1, '10:00', '18:00'),
(2, 'Friday', 1, '10:00', '16:00'),
(3, 'Monday', 1, '08:00', '16:00'),
(3, 'Tuesday', 1, '08:00', '16:00'),
(3, 'Wednesday', 1, '08:00', '16:00'),
(3, 'Thursday', 1, '08:00', '16:00'),
(3, 'Friday', 1, '08:00', '14:00'),
(4, 'Monday', 1, '11:00', '19:00'),
(4, 'Tuesday', 1, '11:00', '19:00'),
(4, 'Wednesday', 1, '11:00', '19:00'),
(4, 'Thursday', 1, '11:00', '19:00'),
(4, 'Friday', 1, '11:00', '17:00');

INSERT INTO Student_Studying (student_id, teacher_id, day) VALUES 
(1, 1, 'Monday'),
(1, 1, 'Wednesday'),
(2, 2, 'Tuesday'),
(2, 2, 'Thursday'),
(3, 4, 'Monday'),
(3, 4, 'Friday');

-- Verify the database structure
.schema

-- Show sample data
SELECT 'Students:' as Table_Name;
SELECT * FROM Student;

SELECT 'Teachers:' as Table_Name;
SELECT * FROM Teacher;

SELECT 'Student_Studying:' as Table_Name;
SELECT * FROM Student_Studying;

SELECT 'Teacher_Day_Availability:' as Table_Name;
SELECT * FROM Teacher_Day_Availability;

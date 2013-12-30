MarkMaster
Windows 8 Grade Calculator Application
Author: Joshua Segeren
Date: December 2013 - January 2014

Windows 8.1 SDK

This is a simple course and grade management application, specifically tailored for use by McMaster University students.
Developed as part of practice for the McMaster App Development (MAD) club (official MSU club for mobile development).

Features:
	- Unlimited number of courses, course items
	- Course items are represented by unique ID (hidden), name, type (e.g. midterm, lab, assignment), weight, and grade
	- Courses are represented by unique ID (hidden), name, course code, actual (calculated) grade, and desired grade

Upcoming:
	- Allows conversion between 4.0 GPA and 12.0 McMaster scale
	- Allows required grades for remaining items to be calculated/projected based on current items grades and upcoming (empty)
	- assignments, or based on the remainder weight of all items for a given course
	- Allows chronological view of assignments, (i.e. time-series of grades by date), target line
	- Social feature for grade sharing / rankings?


TODO:
	- Complete upcoming features AND
	- Implement seralization/deserialization of grade information so app state can be maintained across user sessions
	- Allow deletion / edit fields for courses, and course items
		- Prompt for confirmation before deletion of more than one item at a time
	- For course name, use dropbox, and retrieve list of courses from official McMaster website

BUG LOG:
	?? (None so far?)

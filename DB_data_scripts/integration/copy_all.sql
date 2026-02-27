copy employee_base (id, full_name, phone, email, birth_date, photo, duties)
from '/db-data/employee_with_id.csv'
delimiter ','
csv header;

insert into employee_reduced (id, email)
select id, email
from employee_base;

copy education (id, employee_id, institution, education_level, study_field, start_date, end_date)
from '/db-data/education_with_id.csv'
delimiter ','
csv header
null 'NULL';

copy company (id, title, registration_date, phone, email, inn, kpp, ogrn, address)
from '/db-data/company_with_id.csv'
delimiter ','
csv header;

copy post (id, title, salary, company_id)
from '/db-data/post_with_id.csv'
delimiter ','
csv header;

copy position (id, title, parent_id, company_id)
from '/db-data/position.csv'
delimiter ','
csv header
null 'null';

insert into position_reduced (id, parent_id)
select id, parent_id
from position;

copy post_history (post_id, employee_id, start_date, end_date)
from '/db-data/post_history.csv'
delimiter ','
csv header
null 'null';

copy position_history (position_id, employee_id, start_date, end_date)
from '/db-data/position_history.csv'
delimiter ','
csv header
null 'null';

insert into position_history_reduced (position_id, employee_id)
select position_id, employee_id
from position_history
where end_date is null;

copy score_story (employee_id, author_id, position_id, created_at, efficiency_score, engagement_score, competency_score)
from '/db-data/scores.csv'
delimiter ','
csv header
null 'null';


-- Expands baseline fixture to a benchmark-size dataset.
-- Target: +100k score rows with valid FK references.

do
$$
declare
    employee_count int;
    position_count int;
begin
    select count(*) into employee_count from employee_base;
    select count(*) into position_count from position;

    if employee_count = 0 then
        raise exception 'employee_base is empty, baseline seed is required before benchmark expansion';
    end if;

    if position_count = 0 then
        raise exception 'position is empty, baseline seed is required before benchmark expansion';
    end if;
end
$$;

with employees as (
    select array_agg(id) as ids
    from employee_base
),
positions as (
    select array_agg(id) as ids
    from position
),
generated as (
    select
        gs,
        (select ids[(1 + floor(random() * array_length(ids, 1)))::int] from employees) as employee_id,
        (select ids[(1 + floor(random() * array_length(ids, 1)))::int] from employees) as author_id,
        (select ids[(1 + floor(random() * array_length(ids, 1)))::int] from positions) as position_id
    from generate_series(1, 100000) as gs
)
insert into score_story (
    employee_id,
    author_id,
    position_id,
    created_at,
    efficiency_score,
    engagement_score,
    competency_score
)
select
    g.employee_id,
    g.author_id,
    g.position_id,
    now() - (((random() * 365)::int)::text || ' days')::interval,
    1 + floor(random() * 5)::int,
    1 + floor(random() * 5)::int,
    1 + floor(random() * 5)::int
from generated g;

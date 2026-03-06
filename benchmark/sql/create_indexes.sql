create index if not exists idx_score_story_employee_created_at
    on score_story (employee_id, created_at desc);

create index if not exists idx_score_story_position_created_at
    on score_story (position_id, created_at desc);

create index if not exists idx_position_history_employee_end_date
    on position_history (employee_id, end_date);

create index if not exists idx_position_history_position_end_date
    on position_history (position_id, end_date);

create index if not exists idx_position_parent_id
    on position (parent_id);

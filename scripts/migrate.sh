#!/bin/bash

# ==================== КОНФИГУРАЦИЯ ====================
POSTGRES_HOST="localhost"
POSTGRES_PORT="5432"
POSTGRES_DB="ppo_test"
POSTGRES_USER="postgres"
POSTGRES_PASSWORD="postgres"

MONGO_HOST="localhost"
MONGO_PORT="27017"
MONGO_DB="ppo_test"

# Директория для временных файлов
TEMP_DIR="/tmp"

# ==================== ФУНКЦИИ ЛОГИРОВАНИЯ ====================
log_info() {
    echo "ℹ️  $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_error() {
    echo "❌ $(date '+%Y-%m-%d %H:%M:%S') - $1" >&2
}

log_success() {
    echo "✅ $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

# ==================== ПРОВЕРКИ ====================
check_dependencies() {
    log_info "Проверка зависимостей..."
    
    if ! command -v psql &> /dev/null; then
        log_error "psql не установлен"
        exit 1
    fi
    
    if ! command -v mongoimport &> /dev/null; then
        log_error "mongoimport не установлен"
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        log_error "jq не установлен. Установите: sudo apt-get install jq"
        exit 1
    fi
    
    log_success "Все зависимости установлены"
}

test_connections() {
    log_info "Проверка подключений..."
    
    # Проверка PostgreSQL
    if ! PGPASSWORD=$POSTGRES_PASSWORD psql -h $POSTGRES_HOST -p $POSTGRES_PORT -U $POSTGRES_USER -d $POSTGRES_DB -c "SELECT 1;" &> /dev/null; then
        log_error "Не удалось подключиться к PostgreSQL"
        exit 1
    fi
    
    # Проверка MongoDB
    if ! mongosh --eval "db.adminCommand('ping')" &> /dev/null; then
        log_error "Не удалось подключиться к MongoDB"
        exit 1
    fi
    
    log_success "Подключения работают"
}

# ==================== ЭКСПОРТ ИЗ POSTGRESQL ====================
export_table_to_json() {
     local table_name=$1
        local output_file=$2
        
        log_info "Экспорт таблицы $table_name..."
        
        # Используем Python для надежного экспорта
        python3 -c "
import psycopg2
import json
import os
import uuid

try:
    conn = psycopg2.connect(
        host='$POSTGRES_HOST',
        port='$POSTGRES_PORT',
        dbname='$POSTGRES_DB',
        user='$POSTGRES_USER',
        password='$POSTGRES_PASSWORD'
    )
    
    cursor = conn.cursor()
    cursor.execute(f\"SELECT * FROM $table_name\")
    
    columns = [desc[0] for desc in cursor.description]
    rows = cursor.fetchall()
    
    # Преобразуем в список словарей
    data = []
    for row in rows:
        data.append(dict(zip(columns, row)))
        if '$table_name' == 'position_history':
          data[-1]['id']=uuid.uuid4()
        elif '$table_name' == 'post_history':
          data[-1]['id']=uuid.uuid4()
        elif '$table_name' == 'users':
          data[-1]['id']=uuid.uuid4()
    
    with open('$output_file', 'w') as f:
        json.dump(data, f, indent=2, default=str)
    
    print(f'Успешно экспортировано {len(data)} записей')
    
except Exception as e:
    print(f'Ошибка: {e}')
    exit(1)
    " > $output_file.log 2>&1
        
        if [ $? -eq 0 ] && [ -s "$output_file" ]; then
            local record_count=$(jq length $output_file)
            log_success "Экспортировано $record_count записей из $table_name"
        else
            log_error "Ошибка экспорта таблицы $table_name"
            cat $output_file.log
            return 1
        fi
}

# Альтернативный метод для больших таблиц
export_table_to_json_stream() {
    local table_name=$1
    local output_file=$2
    
    log_info "Потоковый экспорт таблицы $table_name..."
    
    # Создаем пустой JSON массив
    echo "[" > $output_file
    
    # Экспортируем построчно
    PGPASSWORD=$POSTGRES_PASSWORD psql -h $POSTGRES_HOST -p $POSTGRES_PORT -U $POSTGRES_USER -d $POSTGRES_DB \
        -t \
        --csv \
        -c "SELECT row_to_json(t) FROM $table_name t WHERE is_deleted = false;" \
        | sed 's/^/{/"' \
        | sed 's/$/},/' \
        | sed '$ s/,$//' >> $output_file
    
    # Завершаем JSON массив
    echo "]" >> $output_file
    
    local record_count=$(jq length $output_file)
    log_success "Экспортировано $record_count записей из $table_name"
}

# ==================== ПРЕОБРАЗОВАНИЕ ДАННЫХ ====================
transform_uuids() {
    local input_file=$1
    local output_file=$2
    
    log_info "Преобразование UUID для MongoDB..."
    
    # Преобразуем UUID в строки и добавляем временные метки
    jq 'map(. + { 
        _id: .id,
        createdAt: (now | todateiso8601),
        updatedAt: (now | todateiso8601)
    } | del(.id))' $input_file > $output_file
    
    log_success "Данные преобразованы для MongoDB"
}

# ==================== ИМПОРТ В MONGODB ====================
import_to_mongodb() {
    local collection_name=$1
    local input_file=$2
    
    log_info "Импорт в коллекцию $collection_name..."
    
    if [ ! -s "$input_file" ]; then
        log_error "Файл $input_file пуст или не существует"
        return 1
    fi
    
    local record_count=$(jq length $input_file)
    
    if [ "$record_count" -eq 0 ]; then
        log_info "Нет данных для импорта в $collection_name"
        return 0
    fi
    
    # Импортируем в MongoDB
    if mongoimport --host $MONGO_HOST --port $MONGO_PORT \
        --db $MONGO_DB \
        --collection $collection_name \
        --file $input_file \
        --jsonArray; then
        log_success "Импортировано $record_count записей в $collection_name"
    else
        log_error "Ошибка импорта в $collection_name"
        return 1
    fi
}

# ==================== МИГРАЦИЯ КОНКРЕТНЫХ ТАБЛИЦ ====================
migrate_companies() {
    log_info "Начало миграции компаний..."
    
    export_table_to_json "company" "$TEMP_DIR/companies_pg.json"
    transform_uuids "$TEMP_DIR/companies_pg.json" "$TEMP_DIR/companies_mongo.json"
    import_to_mongodb "companies" "$TEMP_DIR/companies_mongo.json"
}

migrate_educations() {
    log_info "Начало миграции образований..."
    
    export_table_to_json "education" "$TEMP_DIR/educations_pg.json"
    transform_uuids "$TEMP_DIR/educations_pg.json" "$TEMP_DIR/educations_mongo.json"
    import_to_mongodb "educations" "$TEMP_DIR/educations_mongo.json"
}

migrate_positions() {
    log_info "Начало миграции должностей..."
    
    export_table_to_json "position" "$TEMP_DIR/positions_pg.json"
    transform_uuids "$TEMP_DIR/positions_pg.json" "$TEMP_DIR/positions_mongo.json"
    import_to_mongodb "positions" "$TEMP_DIR/positions_mongo.json"
}

migrate_posts() {
    log_info "Начало миграции должностей сотрудников..."
    
    export_table_to_json "post" "$TEMP_DIR/posts_pg.json"
    transform_uuids "$TEMP_DIR/posts_pg.json" "$TEMP_DIR/posts_mongo.json"
    import_to_mongodb "posts" "$TEMP_DIR/posts_mongo.json"
}

migrate_employee() {
    log_info "Начало миграции сотрудников..."
    
    export_table_to_json "employee_base" "$TEMP_DIR/employee_base_pg.json"
    transform_uuids "$TEMP_DIR/employee_base_pg.json" "$TEMP_DIR/employee_base_mongo.json"
    import_to_mongodb "employee_base" "$TEMP_DIR/employee_base_mongo.json"
}

migrate_position_history() {
    log_info "Начало миграции истории позиций сотрудников..."
    
    export_table_to_json "position_history" "$TEMP_DIR/position_history_pg.json"
    transform_uuids "$TEMP_DIR/position_history_pg.json" "$TEMP_DIR/position_history_mongo.json"
    import_to_mongodb "position_history" "$TEMP_DIR/position_history_mongo.json"
}

migrate_post_history() {
    log_info "Начало миграции истории должностей сотрудников..."
    
    export_table_to_json "post_history" "$TEMP_DIR/post_history_pg.json"
    transform_uuids "$TEMP_DIR/post_history_pg.json" "$TEMP_DIR/post_history_mongo.json"
    import_to_mongodb "post_history" "$TEMP_DIR/post_history_mongo.json"
}

migrate_score_story() {
    log_info "Начало миграции истории оценок сотрудников..."
    
    export_table_to_json "score_story" "$TEMP_DIR/score_story_pg.json"
    transform_uuids "$TEMP_DIR/score_story_pg.json" "$TEMP_DIR/score_story_mongo.json"
    import_to_mongodb "score_story" "$TEMP_DIR/score_story_mongo.json"
}

migrate_user() {
    log_info "Начало миграции пользователей..."
    
    export_table_to_json "users" "$TEMP_DIR/users_pg.json"
    transform_uuids "$TEMP_DIR/users_pg.json" "$TEMP_DIR/users_mongo.json"
    import_to_mongodb "users" "$TEMP_DIR/users_mongo.json"
}

# ==================== ОСНОВНАЯ ЛОГИКА ====================
main() {
    log_info "Начало миграции данных из PostgreSQL в MongoDB"
    
    # Проверки
    check_dependencies
    test_connections
    
    # Миграция таблиц
    migrate_companies
    migrate_employee
    migrate_educations
    migrate_positions
    migrate_posts
    migrate_position_history
    migrate_post_history
    migrate_score_story
    migrate_user
    # Очистка
#    cleanup
    
    log_success "Миграция завершена!"
}

# ==================== ОЧИСТКА ====================
cleanup() {
    log_info "Очистка временных файлов..."
    rm -rf $TEMP_DIR
    log_success "Временные файлы удалены"
}

# ==================== ЗАПУСК ====================
if [ "$1" = "--help" ]; then
    echo "Использование: $0"
    echo ""
    echo "Переменные окружения (можно задать перед запуском):"
    echo "  POSTGRES_HOST, POSTGRES_PORT, POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD"
    echo "  MONGO_HOST, MONGO_PORT, MONGO_DB"
    echo ""
    echo "Пример:"
    echo "  POSTGRES_USER=myuser POSTGRES_PASSWORD=mypass $0"
    exit 0
fi

# Запуск основной функции
main
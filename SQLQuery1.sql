IF EXISTS (SELECT 1 FROM Roles WHERE Name LIKE '????%')
     BEGIN
         -- Сначала нужно найти ID этой роли, чтобы удалить связи с ней
         DECLARE @badRoleId INT;
         SELECT @badRoleId = Id FROM Roles WHERE Name LIKE '????%';
    
         IF @badRoleId IS NOT NULL
         BEGIN
            -- Удаляем все назначения этой некорректной роли пользователям
            DELETE FROM UserRoles WHERE RolesId = @badRoleId;
            -- Теперь удаляем саму роль
            DELETE FROM Roles WHERE Id = @badRoleId;
            PRINT 'ИНФО: Некорректная роль (????) была найдена и удалена.';
        END
    END
    GO
   
    -- Теперь назначаем ПРАВИЛЬНУЮ роль
    DECLARE @login NVARCHAR(256) = 'Admin';
    DECLARE @roleName NVARCHAR(256) = N'Администратор'; -- Префикс N для правильной кодировки
   
    DECLARE @userId INT;
    DECLARE @roleId INT;
   
    -- Находим ID пользователя и правильной роли
    SELECT @userId = Id FROM Users WHERE Login = @login;
    SELECT @roleId = Id FROM Roles WHERE Name = @roleName;
   
    -- Проверка и назначение
    IF @userId IS NULL
    BEGIN
        PRINT 'ОШИБКА: Пользователь с логином ''' + @login + ''' не найден.';
        RETURN;
    END
   
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE RolesId = @roleId AND UsersId = @userId)
    BEGIN
        INSERT INTO UserRoles (RolesId, UsersId) VALUES (@roleId, @userId);
        PRINT 'УСПЕХ: Роль "Администратор" успешно добавлена пользователю "' + @login + '".';
    END
    ELSE
    BEGIN
        PRINT 'ИНФО: Роль "Администратор" уже была назначена пользователю "' + @login + '".';
    END
    GO
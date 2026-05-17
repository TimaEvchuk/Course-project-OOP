 USE PlantifyDB;
 GO
 DECLARE @AdminLogin NVARCHAR(256) = N'Admin';
    
     -- 2. Остальной код не меняйте.
     DECLARE @AdminUserId INT;
     DECLARE @AdminRoleId INT;
    
     -- Находим ID пользователя
     SELECT @AdminUserId = Id FROM Users WHERE Login = @AdminLogin;
   
    -- Находим ID роли "Администратор"
    SELECT @AdminRoleId = Id FROM Roles WHERE Name = N'Администратор';
   
    -- Проверяем, что и пользователь, и роль найдены
    IF @AdminUserId IS NOT NULL AND @AdminRoleId IS NOT NULL
    BEGIN
        -- Проверяем, нет ли у пользователя уже этой роли
        IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UsersId = @AdminUserId AND RolesId = @AdminRoleId)
        BEGIN
            -- Назначаем роль
            INSERT INTO UserRoles (UsersId, RolesId)
            VALUES (@AdminUserId, @AdminRoleId);
            PRINT 'УСПЕХ: Роль "Администратор" успешно назначена пользователю "' + @AdminLogin + '".';
        END
        ELSE
        BEGIN
           PRINT 'ИНФО: У пользователя "' + @AdminLogin + '" уже есть роль "Администратор".';
        END
   END
    ELSE
    BEGIN
        IF @AdminUserId IS NULL
            PRINT 'ОШИБКА: Пользователь с логином ''' + @AdminLogin + ''' не найден.';
        IF @AdminRoleId IS NULL
           PRINT 'ОШИБКА: Роль "Администратор" не найдена.';
    END
  GO
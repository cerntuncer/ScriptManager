-- ScriptManager: Tüm iş verisini siler; şema ve __EFMigrationsHistory kalır.
-- Yerel test için. Üretimde kullanmayın.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'dbo.Commits', N'U') IS NOT NULL
        DELETE FROM dbo.Commits;

    DELETE FROM dbo.Conflicts;
    DELETE FROM dbo.Scripts;

    UPDATE dbo.Batches SET ParentBatchId = NULL WHERE ParentBatchId IS NOT NULL;
    UPDATE dbo.Batches SET ReleaseId = NULL WHERE ReleaseId IS NOT NULL;
    UPDATE dbo.Releases SET RootBatchId = NULL WHERE RootBatchId IS NOT NULL;

    DELETE FROM dbo.Batches;
    DELETE FROM dbo.Releases;

    DELETE FROM dbo.UserCredentials;
    DELETE FROM dbo.Users;

    COMMIT TRAN;
    PRINT 'Tüm tablolar temizlendi.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;

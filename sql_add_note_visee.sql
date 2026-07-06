-- ============================================================
-- Ajoute la colonne id_visee_fk a user_note pour rattacher une
-- note personnelle a une visee du referentiel (selection en cascade).
--
-- IMPORTANT: A EXECUTER SUR LA BD PROD AVANT DE DEPLOYER LE BACKEND.
--   Sans cette colonne, l'enregistrement des notes echouerait
--   (colonne id_visee_fk introuvable).
--
-- Script idempotent : peut etre relance sans risque.
-- ============================================================

ALTER TABLE public.user_note
    ADD COLUMN IF NOT EXISTS id_visee_fk integer NULL;

-- Contexte complet de la cascade (texte fige compose cote client) affiche au calendrier.
ALTER TABLE public.user_note
    ADD COLUMN IF NOT EXISTS visee_contexte text NULL;

-- Contrainte FK : si la visee est supprimee, la note conserve la ligne
-- mais id_visee_fk repasse a NULL (ON DELETE SET NULL).
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'user_note_id_visee_fk_fkey'
    ) THEN
        ALTER TABLE public.user_note
            ADD CONSTRAINT user_note_id_visee_fk_fkey
            FOREIGN KEY (id_visee_fk) REFERENCES public.visees (id_visee)
            ON UPDATE NO ACTION ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_user_note_visee
    ON public.user_note(id_visee_fk);

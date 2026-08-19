-- ============================================================
-- Cree la table user_conge : corrections personnelles du calendrier
-- des conges scolaires.
--
-- La table calendrier_scolaire est commune et alimentee par le scraper ;
-- certaines dates y sont inexactes. Chaque utilisateur enregistre ici ses
-- propres corrections, appliquees uniquement a son calendrier :
--   id_calendrier_fk renseigne + masque = false -> remplace les dates du conge officiel
--   id_calendrier_fk renseigne + masque = true  -> masque le conge officiel
--   id_calendrier_fk NULL                       -> conge ajoute par l'utilisateur
--
-- IMPORTANT: A EXECUTER SUR LA BD PROD AVANT DE DEPLOYER LE BACKEND.
--   Sans cette table, l'ecran des conges et le chargement du calendrier
--   renverraient une erreur 500 (relation "user_conge" introuvable).
--
-- Script idempotent : peut etre relance sans risque.
-- ============================================================

CREATE TABLE IF NOT EXISTS public.user_conge
(
    id               serial       NOT NULL,
    id_user_fk       integer      NOT NULL,
    -- Pas de contrainte FK vers calendrier_scolaire : le scraper peut reconstruire
    -- cette table, et une correction orpheline est simplement ignoree a l'affichage.
    id_calendrier_fk integer      NULL,
    nom              varchar(100) NOT NULL,
    date_debut       date         NOT NULL,
    date_fin         date         NOT NULL,
    masque           boolean      NOT NULL DEFAULT false,
    CONSTRAINT user_conge_pkey PRIMARY KEY (id)
);

-- Contrainte FK vers l'utilisateur proprietaire : ses corrections disparaissent
-- avec son compte.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'user_conge_id_user_fk_fkey'
    ) THEN
        ALTER TABLE public.user_conge
            ADD CONSTRAINT user_conge_id_user_fk_fkey
            FOREIGN KEY (id_user_fk) REFERENCES public.utilisateur (id_user)
            ON UPDATE NO ACTION ON DELETE CASCADE;
    END IF;
END $$;

-- Lecture principale : toutes les corrections d'un utilisateur
CREATE INDEX IF NOT EXISTS idx_user_conge_user
    ON public.user_conge(id_user_fk);

-- Une seule correction par conge officiel et par utilisateur : le backend ecrase
-- la correction existante, l'index garantit qu'aucun doublon ne s'installe.
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_conge_unique
    ON public.user_conge(id_user_fk, id_calendrier_fk)
    WHERE id_calendrier_fk IS NOT NULL;

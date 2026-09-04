-- ============================================================
-- Cree les tables lecon et lecon_phase : les preparations de lecon.
--
-- Une preparation reprend le formulaire papier « Preparation de lecon type » :
--   titre, enseignant, duree, nombre de seances, niveaux, competences,
--   puis le deroulement decoupe en phases (intitule + temps).
--
-- Le deroulement vit dans une table fille plutot que dans quatre paires de
-- colonnes phase1/temps1 ... phase4/temps4 : le modele papier en compte quatre,
-- mais une lecon qui en demande trois ou six ne doit pas exiger une nouvelle
-- migration en production. L'ordre d'affichage est porte par la colonne ordre.
--
-- IMPORTANT: A EXECUTER SUR LA BD PROD AVANT DE DEPLOYER LE BACKEND.
--   Sans ces tables, l'ecran des lecons renverrait une erreur 500
--   (relation "lecon" introuvable).
--
-- Script idempotent : peut etre relance sans risque.
-- ============================================================

CREATE TABLE IF NOT EXISTS public.lecon
(
    id              serial       NOT NULL,
    id_user_fk      integer      NOT NULL,
    titre           varchar(200) NOT NULL,
    enseignant      varchar(150) NOT NULL DEFAULT '',
    -- Texte libre plutot qu'un intervalle : le formulaire accepte aussi bien
    -- « 50 min » que « 2 x 50 min » ou « une matinee ».
    duree           varchar(100) NOT NULL DEFAULT '',
    nombre_seances  integer      NOT NULL DEFAULT 1,
    -- Plusieurs niveaux possibles pour une meme lecon, saisis librement
    niveaux         varchar(200) NOT NULL DEFAULT '',
    competences     text         NOT NULL DEFAULT '',
    -- Visee du referentiel choisie dans la cascade. Le detail complet vit dans
    -- competences (texte fige au moment de l'enregistrement) ; cette colonne garde
    -- le lien reel vers le referentiel, comme user_note.id_visee_fk.
    id_visee_fk     integer      NULL,
    created_at      timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    modified_at     timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    CONSTRAINT lecon_pkey PRIMARY KEY (id)
);

-- Le deroulement : une ligne par phase, dans l'ordre d'affichage
CREATE TABLE IF NOT EXISTS public.lecon_phase
(
    id           serial      NOT NULL,
    id_lecon_fk  integer     NOT NULL,
    ordre        integer     NOT NULL,
    intitule     text        NOT NULL DEFAULT '',
    temps        varchar(50) NOT NULL DEFAULT '',
    CONSTRAINT lecon_phase_pkey PRIMARY KEY (id)
);

-- Contrainte FK vers l'utilisateur proprietaire : ses preparations disparaissent
-- avec son compte.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'lecon_id_user_fk_fkey'
    ) THEN
        ALTER TABLE public.lecon
            ADD CONSTRAINT lecon_id_user_fk_fkey
            FOREIGN KEY (id_user_fk) REFERENCES public.utilisateur (id_user)
            ON UPDATE NO ACTION ON DELETE CASCADE;
    END IF;
END $$;

-- Les phases n'existent que par leur lecon : supprimer la lecon les emporte.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'lecon_phase_id_lecon_fk_fkey'
    ) THEN
        ALTER TABLE public.lecon_phase
            ADD CONSTRAINT lecon_phase_id_lecon_fk_fkey
            FOREIGN KEY (id_lecon_fk) REFERENCES public.lecon (id)
            ON UPDATE NO ACTION ON DELETE CASCADE;
    END IF;
END $$;

-- Le referentiel peut evoluer : supprimer une visee laisse la preparation en place
-- et son texte intact, seul le lien repasse a NULL (ON DELETE SET NULL).
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'lecon_id_visee_fk_fkey'
    ) THEN
        ALTER TABLE public.lecon
            ADD CONSTRAINT lecon_id_visee_fk_fkey
            FOREIGN KEY (id_visee_fk) REFERENCES public.visees (id_visee)
            ON UPDATE NO ACTION ON DELETE SET NULL;
    END IF;
END $$;

-- Lecture principale : toutes les preparations d'un utilisateur
CREATE INDEX IF NOT EXISTS idx_lecon_user
    ON public.lecon(id_user_fk);

-- Lecture des phases d'une preparation, deja dans l'ordre d'affichage
CREATE INDEX IF NOT EXISTS idx_lecon_phase_lecon
    ON public.lecon_phase(id_lecon_fk, ordre);

-- Une seule phase par rang : l'enregistrement reecrit les phases d'une lecon,
-- l'index garantit qu'aucun doublon de rang ne s'installe.
CREATE UNIQUE INDEX IF NOT EXISTS idx_lecon_phase_unique
    ON public.lecon_phase(id_lecon_fk, ordre);

-- ============================================================
-- Droits pour le role utilise par l'API (voir appsettings.Production.json).
-- Les tables appartiennent a l'administrateur qui execute ce script ; sans ces
-- GRANT, l'API tombe en "42501: permission denied for table lecon".
-- Le droit sur les sequences est indispensable : sans lui le SELECT passe mais
-- l'INSERT echoue, car id est alimente par nextval().
-- ============================================================
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'admin_api01') THEN
        GRANT SELECT, INSERT, UPDATE, DELETE ON public.lecon       TO admin_api01;
        GRANT SELECT, INSERT, UPDATE, DELETE ON public.lecon_phase TO admin_api01;
        GRANT USAGE, SELECT ON SEQUENCE public.lecon_id_seq        TO admin_api01;
        GRANT USAGE, SELECT ON SEQUENCE public.lecon_phase_id_seq  TO admin_api01;
    END IF;
END $$;

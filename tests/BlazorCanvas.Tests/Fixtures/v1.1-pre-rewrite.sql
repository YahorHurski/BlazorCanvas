-- BlazorCanvas v1.1-era pre-rewrite database snapshot.
-- IMMUTABLE: captured before Phase 10's storage-model migration; never regenerate or hand-edit it.
-- After Phase 10 runs, no pre-rewrite state remains to capture.
-- See v1.1-pre-rewrite-MANIFEST.md for contents and expected post-migration values.

--
-- PostgreSQL database dump
--

\restrict z0d8OWCKRGiu4CEr19MQ10nwx1fDcDGtsPGWAyHGdOm06TDIFCOidHHejwdSAhF

-- Dumped from database version 17.10 (Debian 17.10-1.pgdg13+1)
-- Dumped by pg_dump version 17.10 (Debian 17.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: figures; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.figures (
    id integer NOT NULL,
    user_id integer NOT NULL,
    type text NOT NULL,
    x1 integer NOT NULL,
    y1 integer NOT NULL,
    x2 integer NOT NULL,
    y2 integer NOT NULL,
    CONSTRAINT box_is_a_box CHECK (((type <> ALL (ARRAY['rectangle'::text, 'triangle'::text])) OR ((x2 > x1) AND (y2 > y1)))),
    CONSTRAINT circle_is_a_circle CHECK (((type <> 'circle'::text) OR (((x2 - x1) = (y2 - y1)) AND (x2 > x1) AND (((x2 - x1) % 2) = 0)))),
    CONSTRAINT figures_type_is_known CHECK ((type = ANY (ARRAY['line'::text, 'rectangle'::text, 'circle'::text, 'triangle'::text]))),
    CONSTRAINT line_is_a_line CHECK (((type <> 'line'::text) OR ((x2 >= x1) AND ((x2 > x1) OR (y2 <> y1)))))
);


--
-- Name: TABLE figures; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.figures IS 'x1,y1,x2,y2 are ALWAYS the figure''s bounding box. A CIRCLE is stored as the square it is inscribed in: r = (x2-x1)/2, cx = x1+r, cy = y1+r. It is DRAWN centre-out (press centre, drag for radius) but STORED as a square — interaction and storage are different things. A LINE is the segment between the two points and may run diagonally in either vertical direction; it is normalised by swapping the whole point pair, never by sorting axes.';


--
-- Name: figures_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.figures ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.figures_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id integer NOT NULL,
    username text NOT NULL,
    password text NOT NULL
);


--
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.users ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.users_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: figures PK_figures; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.figures
    ADD CONSTRAINT "PK_figures" PRIMARY KEY (id);


--
-- Name: users PK_users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT "PK_users" PRIMARY KEY (id);


--
-- Name: IX_users_username; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_users_username" ON public.users USING btree (username);


--
-- Name: ix_figures_user_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_figures_user_id ON public.figures USING btree (user_id);


--
-- Name: figures FK_figures_users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.figures
    ADD CONSTRAINT "FK_figures_users_user_id" FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict z0d8OWCKRGiu4CEr19MQ10nwx1fDcDGtsPGWAyHGdOm06TDIFCOidHHejwdSAhF

INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (516, 'egor', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (574, 'db-test-3cb3b63ebccd4346a71e3d92fafc1c16', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (575, 'db-test-063dafdc3e19497a807d5326e433e61f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (576, 'db-test-b7b295d73079482b82213df63825a83c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (577, 'db-test-8e8dbf37daa04e9387564ca1bc0914ba', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (578, 'db-test-823641fd81a64fa09263aef9c64cfecd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (579, 'db-test-ff9f5f45e7554ecda283d2ddc686b88b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (580, 'db-test-2a2cf8c00f584397876326ce35471ea4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (581, 'db-test-cbbcb55aa70c4e02b96fe82bb12e738f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (582, 'db-test-52b5c5ba41084534bddfbdeabd6af66a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (583, 'db-test-1713367bb39647f0a1849beb9280ae43', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (641, 'db-test-1f72e11809a241d492e55e422c703447', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (642, 'db-test-1122c966f2eb460ea3372d544daa2ac7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (643, 'db-test-55b8d47b1aaf4b8691e5742886f28ab0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (644, 'db-test-d452c58ae07947d299a63b97f22d2e56', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (645, 'db-test-c637021bd89b4c28b39f88412049eb6c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (646, 'db-test-1786b34d08834181bf6e865421d19bcd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (647, 'db-test-28f09e372b7c4772b46dea4c6e9a41fb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (648, 'db-test-b69118bd7f6041ca8e92eea0eed9480f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (649, 'db-test-fb344e31762a4ee296fe9c4dbd92ce5e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (650, 'db-test-9b7818fb5e0b432f86cd1f232a93c6f3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (651, 'verify-user-1784204163', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (709, 'db-test-a9007ebb6e024c7b80f342aae6999480', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (710, 'db-test-9c672a80638b48329018ef936eb9ab0f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (711, 'db-test-c8b17ce2bd074d39af4274084a61e9db', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (712, 'db-test-5482199ec6be4981a30445a12561b6d7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (713, 'db-test-4d24420041fe4444a2235dd7ee9dac2d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (714, 'db-test-56f94841bfc544c3bc806b557fe90a23', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (715, 'db-test-61c5fed033954c22bc617612aea2f447', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (716, 'db-test-15229292b42c48398836894cb3970908', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (717, 'db-test-385f6bc550cb4faf958d151f8ca9cb5c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (718, 'db-test-44f35914e78a4f7bae80dd0765aa1bfb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (776, 'db-test-8bd3963e96554f37b88d47ef742219e6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (777, 'db-test-b72a720060ee475a9f8e817c43e91ca7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (778, 'db-test-873afa6397e144c897bdd6481730caed', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (779, 'db-test-89bbbe6ea38c472388b08802056d2c98', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (780, 'db-test-5097f2f332cb45caa080b463360f190a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (781, 'db-test-867ef7b8ab9a4e9abc47150f56a43d0e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (782, 'db-test-8deebe08ffa249abb98c81f1345a4b24', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (783, 'db-test-86a1764ff8a042328028237a9d12bcab', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (784, 'db-test-0ecbb100a25943fabf9e2f8a2a1e87b0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (785, 'db-test-a165683efd084e34b122f5580af04f98', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (843, 'db-test-e59c4afb3e644649a011c915059afa1f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (844, 'db-test-abc906d9ffcf458483a575c157f14b4c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (845, 'db-test-a28ae6261718417f97eba23c41771f59', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (846, 'db-test-c9b855db492040bdb0a6a443ff87e727', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (847, 'db-test-184e372e789e479b826e26c1cc66e2c5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (848, 'db-test-4cafca1dc0104a0cae0cd0cdfdb52e08', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (849, 'db-test-222cf6ac000040619d216943bedd5b3f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (850, 'db-test-7d680aec78c34503b10675f1b9fa81f3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (851, 'db-test-fba53e89dda549799668b725cc9c4ee6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (852, 'db-test-44f89f91276046c2a522b583fee79ee9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (911, 'db-test-785bf2d40393410aa5b898a34bc70777', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (912, 'db-test-3acc30aa7f364018b2259371c7b9f417', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (913, 'db-test-7d98b9ee19a84ef0a96048dc3b879f5d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (914, 'db-test-705fe700cc1545a78b4a5ea95d613a21', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (915, 'db-test-cc5167a80e264e1ab11f4fed8c299fa3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (916, 'db-test-1538aef1b47d4c429bb43d689c5a4c28', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (917, 'db-test-02a47b9689fb40138711e50187ba75c3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (918, 'db-test-2221b29fad79450b94c54ee32d977872', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (919, 'db-test-58d6abf3de834ba69ae7ee86238484e0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (920, 'db-test-5bceeca547694d21bd4b429f9a28eea8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (921, 'db-test-f37c222b4b944884a502dd0a146261fc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (922, 'db-test-9dd53b2ce507484cbf60ff85934bf80a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (923, 'db-test-4dad38fa028e4bcb8ebdeebaaabdde34', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (924, 'db-test-cde03faed13e477a8f2017d98287cff6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (925, 'db-test-697a568ce53e44758635337c01374133', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (926, 'db-test-7affc034e846490cb3f7dcddf3d3eb7d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (927, 'db-test-f3208cf1124d421b91750011e2221db6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (928, 'db-test-6b1d032820914554b10f8f00edb40100', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (929, 'db-test-47feeeb8258c4bb1aa9966e2ab7b9d46', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (930, 'db-test-ab68284715e04a20aee4be27708baa8e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (988, 'db-test-ad23a2e557ec433986ee63636a13947e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (989, 'db-test-658b9b4aed5749f79a963637dffff372', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (990, 'db-test-850f639dae25479ba76db94cb14418ec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (991, 'db-test-776d7114ecc3417fa659bb404d32d750', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (992, 'db-test-b634e313072c409597307123407d5cb4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (993, 'db-test-588093c1f94840988ce895320945d0ce', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (994, 'db-test-2546fd099c2e4e5c9c2e018bcba26de5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (995, 'db-test-ff2782873aa74874a05c6d6ba49c553f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (996, 'db-test-56229a1de86f421ebb706d49b6df22dd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (997, 'db-test-b8ab3df98fbb420180ae1d2de041573e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1055, 'db-test-2cdbeca7ea0f4f749aeef35c57265a90', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1056, 'db-test-ade6ddeb66cf403f891df0bfb9dc4eaa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1057, 'db-test-ced743880d794644b3456f0880727e6d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1058, 'db-test-0850e249e2944c4787ad94d07fbc88bb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1059, 'db-test-8ec09744fd55483aad052a7b625db32b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1060, 'db-test-290e4a4119d949d391c724d9aa69a89e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1061, 'db-test-4df194c1dfa24efd86692f7f10ee8b04', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1062, 'db-test-b82994086a4c49278036b812c248e51c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1063, 'db-test-16874dbf959a401baddc2c397baaec4d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1064, 'db-test-d3a0babff20a4dcb846f5f00b6bdec75', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1122, 'db-test-89479da0fc2c480c991224ca8123a1d5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1123, 'db-test-8ec04e4b0a5146dbb0c8bd65a1143e4c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1124, 'db-test-d24c6e999fb845599b0bfb5d2842dcfb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1125, 'db-test-7ef7cea252ec4085a559af522accd133', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1126, 'db-test-e5fdc6dfb94c40459296dd1515bc9906', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1127, 'db-test-d018d38f9f31447bb05f021341ff3cc7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1128, 'db-test-86599f271f67480da02bcb08c5c7e62d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1129, 'db-test-4fb8485feea5432380e245b94150d222', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1130, 'db-test-7816d7732d26446392ad813439b6b83e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1131, 'db-test-2aa83ca076534631aea51f4f19cee8bd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1132, 'db-test-4a204f42049441bcbdec1694c5e40718', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1133, 'db-test-3fc76795e3294aa8aa2820bb303ac0d5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1134, 'db-test-abd713ee514a4bf68b8180a4aa5a76e8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1135, 'db-test-ff7a498beb6544d7829ceecee841502c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1136, 'db-test-81935f52d6e44c24aa438531d47ce1ac', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1137, 'db-test-3114a39b4e684ad1b13e3110aec5fd2d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1138, 'db-test-1a69000bdc7747b68b5b35b53b558279', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1139, 'db-test-260545d9be054dfab06bb997fb2a9449', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1140, 'db-test-fae1ca51117c4367b771375b7e7b9b27', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1141, 'db-test-9fef6d72193b4bd2a2f714dd0e24b036', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1142, 'db-test-3737d91b4d044e63a084161b4052becc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1143, 'db-test-887135e418ae4e69b6a3556b61b4fbd9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1144, 'db-test-8c66c16d160c4fe395ec1e5b8f23364b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1202, 'db-test-46eff0219a0f44bcad65f9e8dbb28be3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1203, 'db-test-6c5f128f15ec47458d64ea27421eb3a6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1204, 'db-test-6239f76082af4c41b872b9fa8795a5b4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1205, 'db-test-745155dd14ee4a41912c5f1a11d3454e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1206, 'db-test-b186e78ee1c747e5b8482e92a42b91d7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1207, 'db-test-70df612f34ff442aaadac23ec2ac83b1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1208, 'db-test-ec0ab7ab6c1949bda10d3dff800cdaa9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1209, 'db-test-8e219369a6964c6d97f04aa817035b38', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1210, 'db-test-4dc1357e286343ec8e2a39a171c08106', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1211, 'db-test-13035adc53814ac784ccdb9b0263f8ee', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1212, 'db-test-e40fd51221944d899a99dff09984c039', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1213, 'db-test-8b371deead154c3ba674b325b8051315', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1214, 'db-test-ed398fb0b31d42999923774e65aa838e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1215, 'db-test-4b939e7dbac2494f84f0b73159ffdeee', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1216, 'db-test-f8979f39638d4e7089ba5e51f282c3fa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1217, 'db-test-08b89c4edb8742c9ada438e1aaad0d90', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1218, 'db-test-619e24e5a4b74c9fb9bfeac8cbb2903a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1219, 'db-test-a096258c0cc649899123fce0bc2b785a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1220, 'db-test-cacf6f063cec476f931e8a9135833169', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1278, 'db-test-2ed48bfa384f40b49f561a2f56e21073', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1279, 'db-test-62b9c3feb02c40888bfbe5bc94db28cd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1280, 'db-test-bc0a02f12230487fbf64937c947e12ea', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1281, 'db-test-567dd517d05d471fb0344e6a43926f54', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1282, 'db-test-eedcae4597b945c5a5d472574b2f28bf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1283, 'db-test-763062a7858641d7873c391e686147fe', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1284, 'db-test-a36107f8d2764fcc9fd446b9f79db76c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1285, 'db-test-9479514a81824488917dbe4937293441', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1286, 'db-test-db4dd1de17e04d8ebff51c3b3920d262', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1287, 'db-test-c7bf65ac93af472c9569e5597dea7d69', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1288, 'db-test-4e82c66b93ec4de7abcf4d173282d2ee', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1289, 'db-test-0fd804723b5349eabc6c33c067918df6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1290, 'db-test-ed06253e88f548a6b3413fa0941a849a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1291, 'db-test-ea7ad1b6cb34460c9684a3864dc98714', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1292, 'db-test-2e533fde07854c7e8ac698c2641d147b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1293, 'db-test-ee07a763c5754ee9ba141eef27d022a1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1294, 'db-test-d9d784d53ade431d9d73d98c80766c7f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1295, 'db-test-d0c2316e59a14ca4b28f9f5af2a83144', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1296, 'db-test-41aedb88290b41d3a9b03f804bb32d48', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1354, 'db-test-79827938d01b4939b625e5bbd862e826', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1355, 'db-test-91e64e63f33040fa81c0d87890eaad33', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1356, 'db-test-af411ca3eae54663873ccb1c20d9528d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1357, 'db-test-12aee56d852f42c5b2b03009b65efd14', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1358, 'db-test-5a805a27ec7a423fb1735e1b2d0668fa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1359, 'db-test-ff5c5ed7693f4f55b7c8bb7396e9da7c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1360, 'db-test-5d2aad5782794f1c9f30d6d6c451512c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1361, 'db-test-b2aee0b321d5448f872a9196ce122875', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1362, 'db-test-f6a66598517e41f5bc40ffcfe57f2a43', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1363, 'db-test-5c1d1e7dcc9243bc9db4f2ca90430e46', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1364, 'db-test-f197e80780f04623b1dfca389b5e6d97', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1365, 'db-test-333d0249c12447b5866e4aa4fb5732b5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1366, 'db-test-26e9888c86e444e5a999487224e2d244', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1367, 'db-test-4565f88d1ed64c98b787fcef847c940f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1368, 'db-test-74c559cddbb249ae8c20040c2ee3edf4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1369, 'db-test-7982eca2eda84c0cbebabd2c74ef3569', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1370, 'db-test-ade4168f6b124726a0b0f85674ad59be', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1371, 'db-test-f615d4c9378749a0ad7d3525f3c56b01', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1372, 'db-test-b2a290da661147d29b3eb3109c08e97c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1430, 'db-test-7efbbb09c8de4e9d9fe287a15784c0ba', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1431, 'db-test-9d6d6faa1be64a849200f0b9a93080cd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1432, 'db-test-44606c0bda254e92932698c20f4b891e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1433, 'db-test-724cbfceada34c3e966f0e47613da5bc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1434, 'db-test-74e3b17358514868b4829f0cb504e057', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1435, 'db-test-55c994ed211745599f35a5f925549bcf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1436, 'db-test-02f2f876bd2a4785b352abbc1174a4ed', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1437, 'db-test-28f6c361fabe4ddaa66f0a8e670292ff', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1438, 'db-test-b774c50adc7340c0b399bd9bf209c0be', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1439, 'db-test-1d66718820ba4a0c8234c881d272c107', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1440, 'db-test-0931442557244c9a9e9f69f6482a4185', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1441, 'db-test-0f411952d7c44c62831c9db0b979bf86', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1442, 'db-test-62878d4c532a41bd8d7a937bf6798443', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1443, 'db-test-a858e3dcecc34a4b8fd1986df460e3ce', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1444, 'db-test-972f5675ea92405c8703aeee7cd7e42d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1445, 'db-test-4fdfbd8f89a84c89bb3c68bce76be142', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1446, 'db-test-4c14b98970764dfd8eea0017d532875c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1447, 'db-test-e4f32c931d494f2883d0c5f57572eb30', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1448, 'db-test-0c12f6b3977042f1ab36a253238b292e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1506, 'db-test-f24a10fe0ab541d3ae4c91dbca48b35f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1507, 'db-test-8c92f87dc15d44f1ad6e3d9f10337937', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1508, 'db-test-d529aa41b9cc48b3abe287e39ea12da3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1509, 'db-test-889f80b718424e6db02a8a90462a9f0a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1510, 'db-test-c0741a50752d44229d5a5e2d50993d7e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1511, 'db-test-9bf2f8f0bca146b0b46756343dfdbabb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1512, 'db-test-9f514e34d45646d2b33d7667f55b2b5d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1513, 'db-test-af064e46761843bd82c6a0ea741ee213', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1514, 'db-test-db48f95662444037b456a171a2622b2e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1515, 'db-test-83299463fe2f442e8ce707933d107157', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1516, 'db-test-fbf9fde7d7dd4dd4b8c9d9379843ce16', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1517, 'db-test-efc68a3298f04d0abfe02afb1574eb40', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1518, 'db-test-8ae033a14ad64ed3ad007aa2515fa1b4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1519, 'db-test-19d59d17356544cfa000a81fff91f62c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1520, 'db-test-2dc48996f209437789904bd37fcf491a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1521, 'db-test-2c7a1bd6a84f425c8bf6c04cf3b825de', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1522, 'db-test-d358e212f75744baa2a52e8c864c3282', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1523, 'db-test-6865dec3997f4631b231491e17792f00', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1524, 'db-test-1aefbb7d7a9e459aa7066cd530abfafd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1582, 'db-test-222de6a1ff9b43608baa0befd6f006e0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1583, 'db-test-88a45bd570184ae18fd449c0efc59d14', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1584, 'db-test-aa9f787841a446f293a8031df48d546d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1585, 'db-test-d943579215b4416a8af49136da18be63', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1586, 'db-test-cf84202339e74b25b7223e381c1edf7a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1587, 'db-test-bfebd71f9781477b9ddb88df0278920b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1588, 'db-test-f605faafb7df467191f85b19c212d9ba', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1589, 'db-test-716bdbe359204e9493b4375864955975', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1590, 'db-test-a61128f9ab294f59b400c1332dca0100', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1591, 'db-test-bfa112a32f9243c7884e47b999416e78', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1592, 'db-test-4e9385d11c2c4c49934b81e9ae3d7145', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1593, 'db-test-38e8ad1e6edd40f2bb5c352da2048c70', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1594, 'db-test-92123f2ac0f540d7956d413cce64e50e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1595, 'db-test-cdddffa848924843a4fb014574d2c26e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1596, 'db-test-201a2e395591424abdd2018259ad1a6c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1597, 'db-test-91b60bd034b34ecfa7bf0b92c9278c5b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1598, 'db-test-fb292f7304134b20be9ff72574f468ef', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1599, 'db-test-2e68b1cb8baf4c1ab1657af74ccb693c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1600, 'db-test-35f30a1dcdea452da1d47b80aeb4ec8e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1658, 'db-test-5add91d4d22b4523a100380582c9d99d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1659, 'db-test-780262f7f0cd454aab237c0ca94f09b5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1660, 'db-test-f38ba71fea8a4061b96901fbbfa33d08', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1661, 'db-test-4cc0070dfbfc43fba3bcb55731d611a5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1662, 'db-test-d42c526ac7f1457d8016b52e60354ee7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1663, 'db-test-b9e5411c67dc44739b34164e016112f6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1664, 'db-test-053a8cee6e48496f8d49c30a837c4565', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1665, 'db-test-4e4533991252456daf92aeac1fc37160', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1666, 'db-test-a50934a49a314ccaa2aa984df70378fb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1667, 'db-test-589e765a52c94e2bacd9d451b15c5d97', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1668, 'db-test-cb11ffdff8b74c8b9bca9326fb0103a9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1669, 'db-test-2386fb23986144a7ac19315b46956cff', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1670, 'db-test-2bb91cc494d34c76b84707cc84cc6330', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1671, 'db-test-f071ee31fec8449fbf13cb0b22db540a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1672, 'db-test-7886f85cd07d421d83d72c0d10b8c905', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1673, 'db-test-0075672117c4487e94bcb95fae0c121a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1674, 'db-test-ca301d89f7da48b99151694eff50efb7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1675, 'db-test-fbc5337108f44d5ab494f4b67308b171', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1676, 'db-test-5daf30695f18468d8d70b22100793f63', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1734, 'db-test-9ff893432d1a4588bf66fe11fee0f051', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1735, 'db-test-2300a3b3838f414db6f5f87743d63636', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1736, 'db-test-957815c6961b4920bd8659b7d61c4838', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1737, 'db-test-068c2ab44e44497da6f61115a8aa6875', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1738, 'db-test-43ba95a10f4d4d67b51aeee6329d737c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1739, 'db-test-d6087b2436af41c98f2f022ed8d075b0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1740, 'db-test-ea79a628c0e44334ad652515715359c9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1741, 'db-test-a23a4984b7094a548b053a4ea73b9d01', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1742, 'db-test-4ad0c4b664b543ec87cfc2ea7701c01c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1743, 'db-test-5156d318d42a4c8ea303dbd09b5f2181', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1744, 'db-test-7d2b2278e22341cf953c7f2b4fbc7576', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1745, 'db-test-e8fa0d61fd0f461d820720991d2970f3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1746, 'db-test-0ffde1e21fc64e679e8eeb2e5bf6416a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1747, 'db-test-040648d0f4bf48099172eaedb64fee68', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1748, 'db-test-0b8736077ec84081b0f7c622296dfaa6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1749, 'db-test-b233135c9d9a430c823d2ee9bc468f56', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1750, 'db-test-5e98b647ee7d4d6eb822ed152eb9569d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1751, 'db-test-ad1f46061078497496c6d2fe5d33dc05', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1752, 'db-test-c6c4b523dc5141729cb9177087afd2d5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1810, 'db-test-648978b39c8b4ab7a896f39a9b959beb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1811, 'db-test-721af25810cc49af8d5ad466f3dd181d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1812, 'db-test-83a0b248ba83413c88bf510841157af8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1813, 'db-test-feadf3f2b12d43bca6478c2f05630ec2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1814, 'db-test-126f37e897df4e1f891183370d5bb0aa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1815, 'db-test-d12b228948874c738f2a58933f10d8d4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1816, 'db-test-e8f33450395744a6963c753cb76fffab', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1817, 'db-test-86f39aa4e56645ecadab9b77898a93f2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1818, 'db-test-ebd899b94f924b5896d70e4e89c37993', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1819, 'db-test-d4a68941a6b04ba49e5d7a41ac4f19ca', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1820, 'db-test-fac8386034f345c69c4e9655d5b4ba0c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1821, 'db-test-8fef5289b1074638b58d41def0f52a00', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1822, 'db-test-a6a25adcac854a1aab5a0a760d7fa6c9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1823, 'db-test-44d9d5275071448aa7370e6a7aa61e4e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1824, 'db-test-161d4976950a4b6b8607c8328b4d87a0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1825, 'db-test-4395ba9164314735bdbf5c0d43391a4f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1826, 'db-test-d8a827ff3327495b95fb6c375e482601', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1827, 'db-test-99c2a84557f4432ebb62e0aa7953bb63', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1828, 'db-test-cbdb10b9acc44f4c863b09fbde1fbbc1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1886, 'db-test-dea1d13e2cec44db996970972439ae04', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1887, 'db-test-cc6c08edadf74e1fb393c86962aa26c1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1888, 'db-test-660b077b610840af83c7e235a100500a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1889, 'db-test-59239c1a970a4e43817acd5b0cd09e8d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1890, 'db-test-5cebc1b74a1b45de910b6cfed2f63feb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1891, 'db-test-b2206e8d0ef3460cb9d3005bc344b4fe', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1892, 'db-test-a592444183e14fd8af2de3e58f494efa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1893, 'db-test-7c341bf2475342aa97198f79d7feac78', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1894, 'db-test-5d9b34b0740c4a3ab44fa24fc8a656be', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1895, 'db-test-53480f20c3ec4f9c9af52c9ee695d1cb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1896, 'db-test-a51c2bb34c824513a30a740013070c6f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1897, 'db-test-6a6511ad1e1348e69711e67d7bc9273a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1898, 'db-test-1712a8aab38e4076aeb78dc7a94d8f3d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1899, 'db-test-86dfd1c4263140f29c9efff37693016c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1900, 'db-test-4e0e7568de9544599c8ff8a2b320c3b2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1901, 'db-test-59b5a49333214ffdacf719860ee82db5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1902, 'db-test-7f93955f1fee48978031206d195bf97a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1903, 'db-test-5ccafd3c047043019b3367e54f65cb9c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1904, 'db-test-50283151367c42288e0ace637dbe761a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1962, 'db-test-73fcfb303cb64217b3abc369ca063597', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1963, 'db-test-e12d546f3f7b4339ad46f9204f93cac7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1964, 'db-test-ef23b15537464cdc873fe4a2a0d6cc63', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1965, 'db-test-5796f25a42cd44918e4fa1e1f3dd8f6e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1966, 'db-test-72f0d7df1bc44a8e819a4f064d1b96a6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1967, 'db-test-1f6a1790959142728b9a896cdc9d7405', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1968, 'db-test-cd7df883ffce4ae08b12e7c9a90907e7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1969, 'db-test-a17235a939844f488d081e6985502196', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1970, 'db-test-3f7a790c08b14f2793a649c66b75ab46', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1971, 'db-test-bc523fd5b207464581c5da9e7f6778e6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1972, 'db-test-be54a4055f7e4ac0a5909376fa96b9b9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1973, 'db-test-5dc2147eda6140bf9a253436b6c06142', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1974, 'db-test-d5dd894c496d4972838a3c840468db70', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1975, 'db-test-a9d599ed4be84ea69dba1ea54adc4495', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1976, 'db-test-559ca36a340b423daee2d21f1b2acb3d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1977, 'db-test-d8f2971f88cb42e798ee160c0e6fd2b9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1978, 'db-test-530dbb7e89434d97b68ade26dd770c26', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1979, 'db-test-05194cfcf9c744808df52094911151c7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (1980, 'db-test-1acaa9e77b8c40b0b6b55a3d6ee9881f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2038, 'db-test-f9058e2fa3ef443fa5c2c3c051967ca5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2039, 'db-test-dc68706d30ff4ee39df7d1e8add5cd6c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2040, 'db-test-3c4c59cce2dd4422aee1e3845ba205e3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2041, 'db-test-8787274cc3364f1c91b3b89811ac890b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2042, 'db-test-dbf8f64c270c4d508b27bfcb3902f801', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2043, 'db-test-d974a01f0e0640cdacd52470ea72eeca', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2044, 'db-test-5f519a4120c64565964da6550e0cbd22', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2045, 'db-test-dfd8ab8d5e924937bddbac64ae6585fb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2046, 'db-test-c231da38d5d84b3eae2064dd65b567af', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2047, 'db-test-5811e5bdd7e146dfbc28d348807a65a6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2048, 'db-test-994f26a732b140eb8e46cdba7b392d33', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2049, 'db-test-5bb8525461704697b384b3117ae5128a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2050, 'db-test-6869cde660db454badb4fc92ee09d44c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2051, 'db-test-a3a2a1bd9b57411f9e33aa708361cdac', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2052, 'db-test-9197ab904cc74646863d252c885cfc10', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2053, 'db-test-64572b931a2f4d72bd9a2b8c2be5bddb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2054, 'db-test-dee708170d694e4799677a3bedd307bd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2055, 'db-test-06cc8d5e98ea418b95f44e689036ac9c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2056, 'db-test-887755fcaebd4316824e47a424d4a9d6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2114, 'db-test-ef70288aba3149ebafa95cbd1cd7a65c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2115, 'db-test-6b688f7ee1f2404781e86f383512f436', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2116, 'db-test-ee60eb9a0a7043f5b05fd910eefc68f5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2117, 'db-test-5dfca1ed14f94c8a8c06dadfd3af93a3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2118, 'db-test-0d56a03c24634fcf9304c523e9b19bef', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2119, 'db-test-b8a020a9a0e544c4969b8e07ef173326', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2120, 'db-test-a84b98c2fae0479085f9a042eea723ec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2121, 'db-test-981ef6a9d0f34526949cba963ba44db4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2122, 'db-test-f19fcbe229c4408bb7f5f45117079e0f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2123, 'db-test-89e8501acbe8479b880093eac7a12519', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2124, 'db-test-063b10a906db47a191da2cdbdd2d2a31', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2125, 'db-test-c8dd5ebb28654000a1f431430b3276ed', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2126, 'db-test-e3d75de5160740e1ba2531096c970fd7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2127, 'db-test-ebd6ca928d9b42bb93fac6f7661dbd47', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2128, 'db-test-d5bcaee6e09f4dabaf4c3b0c5dbf67ec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2129, 'db-test-69304ea22dac44fe94aa7d042214b94c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2130, 'db-test-c4f682ee7ede4c6b8ad75adfe4268fc9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2131, 'db-test-a1370ee4cf37435eb35476c679984e1c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2132, 'db-test-623b0f48936b48b0a116836c0032c73c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2133, 'gurski', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2134, 'gurskiy', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2135, 'hurski', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2193, 'db-test-8e1ca9551e304b9d98f0707d9217cf54', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2194, 'db-test-722ca859d8ee47a3ba555974306105c6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2195, 'db-test-3c0f086a8409450b9f12f363e4c23abe', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2196, 'db-test-2ed19791cb6548ab8388c99ede8bb1b6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2197, 'db-test-4fe247a1275b44a3b8aac51da66451de', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2198, 'db-test-83862a33c4934ba1921341adf970a8ce', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2199, 'db-test-af9f2c4ebd51447eb359c94e161f0a38', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2200, 'db-test-ca36197f4dc24349a52dceffc1df2bba', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2201, 'db-test-ba21b5c14dd34b018ec11eed37b62cce', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2202, 'db-test-8213e87f0a3c423d94f108709eba8555', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2203, 'db-test-404ba265a0ba47b18438c6f7d9441c89', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2204, 'db-test-1a148191259f43089fb23318fbeb649a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2205, 'db-test-a55977700601458eb935546823be7449', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2206, 'db-test-b6b9da3b9b1049e282db8b93622e8592', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2207, 'db-test-6e2f818b7f12452d8a861df314177d85', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2208, 'db-test-88f648e004944eeb879e122f762c6e70', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2209, 'db-test-2d386500e83346cc80833b6aef886359', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2210, 'db-test-270f9c253f6b40d5b601863674f0891a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2211, 'db-test-205ec51a8d1949b1ad38115a9ac7999b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2269, 'db-test-e65596a8a32447b2b3dca3bc80fb6afc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2270, 'db-test-2733398d2dda4e9da2fe0cf1c1678d37', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2271, 'db-test-8f71edf591d84315bf6f6405fd58fd20', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2272, 'db-test-17feb408f8834a07a814bc26a11adacf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2273, 'db-test-13ed2350b9184257b6f73fa8c4306eaf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2274, 'db-test-bc77c87acdd142e79e04908d5217696a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2275, 'db-test-0aae8eb8a9a748c8888180b06a8c8457', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2276, 'db-test-78b83019a5c144aab10ad9127580589d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2277, 'db-test-d0491981f2ce48d58082d0211c7548fb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2278, 'db-test-0d73673c65b34051acc4baead1b6bb96', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2279, 'db-test-dc7465a771f94c36b72c1139ee7575a3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2280, 'db-test-1743c92c37f647c9a84488cb3c6acfe4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2281, 'db-test-6efc55639d7b469ea6b4ae0fa5f323aa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2282, 'db-test-30a329ce1d2a44d4910b684ff51e3830', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2283, 'db-test-08f176d29a734f1d8e309fe1efaf0288', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2284, 'db-test-0b28cd5f4968426f91705ed66f2c0651', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2285, 'db-test-3a53ba1d638e4f329c3305dc22fc978d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2286, 'db-test-231d677f998c4150ab9d13a0d100c765', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2287, 'db-test-ebcc14450f424559aa50f85aa47039c2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2345, 'db-test-8fb8bb087bfe4329947a18cedd77c256', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2346, 'db-test-a6a1e301167c4caba6381c08c9a636a0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2347, 'db-test-53897af41efe45008d30bd2ab389edca', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2348, 'db-test-42cb9383a9554637b8780d24b6d243a5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2349, 'db-test-2281a877cf6d40289e7da18f6018236d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2350, 'db-test-a27784aa105e4aae8ab0f26b77f0530d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2351, 'db-test-3906922685d448149f98e910b60dc6f1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2352, 'db-test-3c1c0772e8c64a8c88e4e5e9dc2b6f69', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2353, 'db-test-a499fb4a6532408094d187d31b07dcde', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2354, 'db-test-fbcb0aa71ab44474848535d22a20c37d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2355, 'db-test-9aab76fea6f24c438fe36ec02b79b71f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2356, 'db-test-acd04355e7884ac6a8439c612bebc91c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2357, 'db-test-23e90d479d814da1bd360a8baeee84bb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2358, 'db-test-2797b67374d54bfa9ccd577c58da2011', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2359, 'db-test-08ebcc4ca0fa42ed81d6958d19574487', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2360, 'db-test-f7baf0b9801c489abc0d92cff835eee8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2361, 'db-test-94e6d2a061f743afb2af7fc3820cf553', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2362, 'db-test-6f3356c0c6db47beb8f4b755a11702a5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2363, 'db-test-ac31127c98b04a018a605f8fa70400ee', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2421, 'db-test-9570e9adcffb4739bc78ada5672b9114', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2422, 'db-test-356bd65c0ffd4cb1a1ac886e51012345', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2423, 'db-test-1d519b4616c64c06ba7c2a8004ab259a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2424, 'db-test-8ede56c9929740e799cb9d074a83208b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2425, 'db-test-d562394ab0f04b5f931b8a7b4969c9b3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2426, 'db-test-3a71d2b0a6d949e4a6a9d937c9f4e827', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2427, 'db-test-6e01d0caed4340269ad6e15ce25b5e50', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2428, 'db-test-7b56ebaf9ba84dba97452e342c2beef4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2429, 'db-test-34c83428d6664a108813502b6cc3786e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2430, 'db-test-79583929f65241fc90338a619fc2179a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2431, 'db-test-af30c6693dd54382aec04f705372b72f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2432, 'db-test-0f9078fa4b6b44fc8c17eb63080dbd0f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2433, 'db-test-9f34a847080b4fca9d7e88c075ce5063', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2434, 'db-test-7430154be37741fa9a2bcb70bc209bf7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2435, 'db-test-9e4083f7e4a54ed08fad6855bb2bd1c7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2436, 'db-test-a15e1189b4d24f65a322e0aa9704862c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2437, 'db-test-83b8b654409a4041bbfc9f6f76f35265', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2438, 'db-test-d1524942d4b84a30b16fa2df15683122', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2439, 'db-test-ce4fe2a6a78d4544bec392b6b7136a01', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2497, 'db-test-ba6bb722c0214972ba0197123e57b773', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2498, 'db-test-2092a65c11104f68834b6c5bb3cd5976', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2499, 'db-test-941ddced8e9943339011eb274e621f9f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2500, 'db-test-fe2955b4ab0446b485c18d13c7f56f1a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2501, 'db-test-8a430752bd264b4bbc43b788cb2e1eb0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2502, 'db-test-d6ed0338dd0e4d0587a3748eb50a4ffc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2503, 'db-test-348df4c5511141ee831e99fa65eb880c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2504, 'db-test-281fcc484fad4a8ab171717f763ab846', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2505, 'db-test-db3aeae8851b44bfa8e4d8ae5f128545', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2506, 'db-test-92893b3dd6374131beb15ab5748dd712', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2507, 'db-test-4efa594984204f709037ee674237666a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2508, 'db-test-d8c29621fc9a48088702ab27a859b8b0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2509, 'db-test-28c5958bd14d4355b1663e35157d0f5a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2510, 'db-test-1126e736e4314111bacc5b59a5766a58', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2511, 'db-test-8bd0500385a1475aa4517c57f0b7de40', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2512, 'db-test-09c9570281704f648f7dd8457f568d6d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2513, 'db-test-146ef04c1f64476b8ade6693cf25b6c5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2514, 'db-test-b3b699db6eba4f1280013c0952a68a13', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2515, 'db-test-2b97aeae17144732a8504a3926fcdf1a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2630, 'db-test-cb49ad62a0174cca9b348066c7943e66', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2631, 'db-test-fd9a735b7c574c68bf2fb2216b12549b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2632, 'db-test-f9eadbfafea94190826cd3488d7c2b58', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2633, 'db-test-1c5a20d98199427a8234fa9fb8c0388b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2634, 'db-test-590af64d44824280b34f4132b6002e8b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2635, 'db-test-03c847ac67634b228a6e09eacc2dffa9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2636, 'db-test-6ba8f637aa5c45d289644c19c7a0f970', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2637, 'db-test-4b3c1a9401c847e184261cee2c89429b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2638, 'db-test-78168c86fce44aa1a8b41b5cf1706d05', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2639, 'db-test-9bf53b7d53db4bc3a7f9999a4075428f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2640, 'db-test-507dee8f4a3e404c9c316e11555b8c9a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2641, 'db-test-f22688356fbf46b2bbfb8106d3c0e449', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2642, 'db-test-635a38b37ebf48e0aee11a6bff973012', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2643, 'db-test-44a3fb17fc5148fcab50d955afcaa0e5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2644, 'db-test-7311dc1ecd2248cb92655ba08ab4f3b7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2645, 'db-test-cd9d7d9457cb46edadba959fc2d2cf1e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2646, 'db-test-b0dc30e00904493b9633e65db2007ca1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2647, 'db-test-6575bb94dc104c1aa556e5dfeac874dc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2648, 'db-test-6dec03f758514648b475b83f161da7fb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2706, 'db-test-87ae98eabcf64e7fa404ec86d67dfe12', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2707, 'db-test-b361081f7e8042a0972c1278774ec0c3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2708, 'db-test-168d2e5b2f0b4f59a477af7620f87b93', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2709, 'db-test-11ef0f0253c3476f9890a244dfd6bb17', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2710, 'db-test-b963c63fccfa4cc0b1170ae2e5285ea6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2711, 'db-test-708fc165e64d40d5a7b1e84a52628af3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2712, 'db-test-1c2487af414940ea9abc852e029d6f27', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2713, 'db-test-78a42ac4234c4ed3982a0811171b5a6d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2714, 'db-test-b2ac732b946041fa8c8e91d7776045e6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2715, 'db-test-9f713e5cdcb64547a905e0cc3e53775b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2716, 'db-test-9734d16b09eb42eda394c95980ec4466', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2717, 'db-test-f55e552e885841c68294019478a0209f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2718, 'db-test-e731cf3e89794741af526a14a0899df6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2719, 'db-test-6c153d6a8b694664ae1f52550eba5c87', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2720, 'db-test-d87061f4a815431fa87011736f6d5dee', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2721, 'db-test-f48ba1bb34994b138e857545d9953b07', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2722, 'db-test-8c2226acd4d3454eac1c60f0dca78efd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2723, 'db-test-826a1186d5434f86a4cb7d2c7bdede10', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2724, 'db-test-fd3dc37dd34e4b5a9267c85d4bbaed1a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2782, 'db-test-1115fd486b4c4cd4b6a3b49a28883e03', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2783, 'db-test-ff76b9d17a0a4972b55cbff772e9e10c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2784, 'db-test-911e83449f53412f922dc12f1124a19d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2785, 'db-test-015daee5124b46deba1482819cd93d8f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2786, 'db-test-01e8856d02894c769ed2ac7993b50d38', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2787, 'db-test-c5d1d41c504b478eae9b481e356828c2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2788, 'db-test-7085a337b4994f349810e858f1bb91ef', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2789, 'db-test-204b8f1839ba4d1e99387a13092ada44', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2790, 'db-test-9ab093f834304aeca9e62d53992476b9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2791, 'db-test-afa4702999854109ace3a709ad8469ea', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2792, 'db-test-0adcc7d32c3b4645b4c207f2fb1db563', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2793, 'db-test-2dd742a350a740a6a95b3d43d8830d6e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2794, 'db-test-58d30c880d9e4d7d96e0df6fff233521', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2795, 'db-test-e70d852e7fb4450c9e62fac05cacf757', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2796, 'db-test-1d79d2f31dd0431e845eafc6c43ca7e9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2797, 'db-test-0b2b760afc6a4f3b979e2c7689255dd0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2798, 'db-test-b929f66242254abb87782e6c6055a723', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2799, 'db-test-6a6d29959f324cf1a85a086cf4e0a985', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2800, 'db-test-a8bc3dd64f7142d5a71ad8c60d9ffd6a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2858, 'db-test-d50a31f0bd454c6e9fd088dc77e1d588', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2859, 'db-test-6765f3e26b36452d8fc665e610cb4028', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2860, 'db-test-87ab0d47d1954822a195b573b624dca9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2861, 'db-test-e0ab5e95d11e4054bd0834818e6f1fa7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2862, 'db-test-76a66d28bd5949e6946691a7879a5daa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2863, 'db-test-236dcf5a1b4c4749b805818894a564b0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2864, 'db-test-ab01d2c744b64c4a817a532c36bfd151', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2865, 'db-test-0d19adf471f84a3bb54d7d127833973e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2866, 'db-test-935556ea270145178cd0ef41b0e1bc7a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2867, 'db-test-e7b09ace50fa4b81a0b88bb4d10e9aec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2868, 'db-test-1f9aa10fb2d540aa883f63044725d25f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2869, 'db-test-34c1af787e7448a08582873163c3f864', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2870, 'db-test-8f6a8b82106a48de9cc22144afef0118', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2871, 'db-test-7399c4a604e54873b115e08b8e3bf5c7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2872, 'db-test-75df9334ea1f440b927e52bebab39864', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2873, 'db-test-2f9f0fb98b9b4f72a65462466c7c6ff8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2874, 'db-test-71ab7f6bcbe649048f5f058874df6214', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2875, 'db-test-77e494e7362f42ee9ed20a5c0f5e3d2f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2876, 'db-test-2a6ba702ab814e1cb30e9eaff7ed289f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2934, 'db-test-cb02c0ff5930444380b8bb4a0025c8f9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2935, 'db-test-27dff1506c3a4d1a95baae411f08c70e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2936, 'db-test-189e7e6c09ad466bbf4d0d038dd2e8e9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2937, 'db-test-42e85306d50a4ba59a1b53f04e7f4ea9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2938, 'db-test-3504a5848210486a9a447a26b9bf630a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2939, 'db-test-3eb628200744455caf17a797dc7a798d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2940, 'db-test-be1e408f93ec4c64b3eecd2ab4d99c0e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2941, 'db-test-277cf5f6f75d4539a946ae542a4e03fb', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2942, 'db-test-77bf3f320a684561885e1dc2b30ef484', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2943, 'db-test-9f635eadc0c8429db2a6fb57ec1be2e5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2944, 'db-test-29e058ea9a934bbe85ad0ce31fda39f6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2945, 'db-test-94c6eecaf3294edeb98d298b3a52a5e9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2946, 'db-test-cab57ec0727e4468ab1f5299d09f6f3a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2947, 'db-test-c6e883c9303842f0bdf3697ca390b905', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2948, 'db-test-a439d5ba6fd64be3b52215751c56470d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2949, 'db-test-9b6036c577864a7a969d8cc8e921147d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2950, 'db-test-2dc559c77b4c458ba17992cb19cbba6b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2951, 'db-test-43ffe1f66cbb491fbc41e5eb1e5da4cf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (2952, 'db-test-e83a395ece334064bd538944c69806cc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3010, 'db-test-fc3c262df673428c8d3bdd2cdc806148', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3011, 'db-test-750ff3623e9b4a34ab1c5c72718c58a2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3012, 'db-test-08516c61109245e3967ab8f97d3325f0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3013, 'db-test-9d39531955684be7b74c8c5630415333', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3014, 'db-test-ba77ef07b66449aba039190f68d35184', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3015, 'db-test-5ff355fca1674d6eb5a01e5614ec5a12', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3016, 'db-test-f51595f521a54acd88420d2653a038ae', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3017, 'db-test-59bfbebe88e141d58b1ff73ca3046ae2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3018, 'db-test-17ca4597695d4a3784b6575b7843f6e6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3019, 'db-test-fa00d41e39654add839c9b9cf3ea3bad', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3020, 'db-test-5d231b290ed844039d1a44c29ffd2e52', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3021, 'db-test-c21b9586b8f646e1a7db5f0abca71449', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3022, 'db-test-ac7c137ff0aa43ebbb40db59f08aa4c1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3023, 'db-test-9d93e3b9ade24082b66f463088c2c6d5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3024, 'db-test-d192caec8a1546a9b0bbf05784b2837f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3025, 'db-test-2d7418a4f22c4aaeb5007c486a5e041f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3026, 'db-test-07522081288d40a5a75908683bb94140', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3027, 'db-test-ff307b68e81c4fe9ad8f5951a06ba204', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3028, 'db-test-8d1f5f34ab9a4efd9cc62e06e65c4a7e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3086, 'db-test-55dedbaa9c27489786527e286a2a8d4b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3087, 'db-test-b21a670f9e3744e3990966d7df449255', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3088, 'db-test-1a0233377e9445eb84f0e1c62ee661d1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3089, 'db-test-1956fb38d55b4d5ab14564290f427ed4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3090, 'db-test-d49ce418126a4d62b8ddbb3cb345606b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3091, 'db-test-dc1714de401f4b9b934bdc2bbfddbf72', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3092, 'db-test-530e1387ea6f4e16bd4b08fac0cbceea', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3093, 'db-test-21dfb4e4333e4fdc811ba0cbebdd12f6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3094, 'db-test-5f82cce17c884c43b9f8b85cac90e914', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3095, 'db-test-3a559a693f8c48acbeb970194c2e9b53', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3096, 'db-test-771f07df1e0545e292897db57a81a06b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3097, 'db-test-0204d370e1d44179ac0349b5dcc388dc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3098, 'db-test-189b1e82ea05402683040cfc4ab66d77', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3099, 'db-test-3514284db9c6413e955c82e7aa9ef07a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3100, 'db-test-2f74212f482143ee8c8d2f44a20c484d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3101, 'db-test-4ed2d51baad541bea68c93b88348e734', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3102, 'db-test-d408e8061e8748c9b600208e09b7d3c6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3103, 'db-test-34a83190a3744ac4a7388be8349a862b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3104, 'db-test-75cdabf3b1064d04b54ccf5fc163b641', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3162, 'db-test-2e999e968c964c89af941acd11b211ac', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3163, 'db-test-2234689534b743bdb84bd650f1099c82', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3164, 'db-test-53211720ea274207bae82ef97f9f96fe', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3165, 'db-test-164737048b5943c7b6eb5cd4fedd48dd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3166, 'db-test-7ac1d2b8471e4951bfc9c85bb49c4f39', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3167, 'db-test-8c827a873f704e6194892c896a0962c4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3168, 'db-test-c6df098b45c6445cb34ba3790b8531f1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3169, 'db-test-b0dafa1be9f94f3d84f1e8c4a21ef9ba', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3170, 'db-test-bd790ce157c4451f904e9b98fa7fec8b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3171, 'db-test-bb9ba79438114a56abd4642455d7e225', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3172, 'db-test-123eea3ab5504211a91387439e8bd62f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3173, 'db-test-a3ebea58ed0e4642a72abbc73ac9c0c8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3174, 'db-test-1847b808ed6f4913bdc22142e2b419c8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3175, 'db-test-98f845266cbf4f99a2f8c3f2e7ab0cfa', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3176, 'db-test-175a22c11767485cbc0c6f417ae22863', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3177, 'db-test-f5c00b436288422f8557f9c9ec9dddd5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3178, 'db-test-04698af96bfb4831a3f413f0a53120dd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3179, 'db-test-34b81b16956247fca2a024bc987bf42a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3180, 'db-test-d9b4c396f65142d9bda9686dc59fc4e4', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3238, 'db-test-7e958420d9d94f0f8be74eb44e548b4d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3239, 'db-test-83b1d32b9fdb4f13ad21a51d690c328e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3240, 'db-test-890b1888bab647a68a0f166ce6665261', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3241, 'db-test-db105d744aa9446f977e94a5a095150f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3242, 'db-test-6c46f622cae0457abc8bfe8796cff074', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3243, 'db-test-25675db211b04f8296be6a17e03492d2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3244, 'db-test-a92b445b7d5040f19eda8a4a972a12a5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3245, 'db-test-ef762bd9cf0243459fdbc89035b43818', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3246, 'db-test-9508b241c29340fa82a4e2158f8b0e24', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3247, 'db-test-1508ab4eb1934051b0e16e7c9bf332d3', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3248, 'db-test-a858d6c574254f1ba5bd3a756530cf5d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3249, 'db-test-980d49a0c7444e2f818121c6714c25ec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3250, 'db-test-b042696eb6234dbabbc060ff9c584e76', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3251, 'db-test-9e50618054884448a88fea974368a0ed', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3252, 'db-test-4cacfa7ead7143299f7266a3448541da', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3253, 'db-test-26c5531688fb4e38bfd235027832188f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3254, 'db-test-89d9f383eecc4f1caebf813d142e2f3f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3255, 'db-test-cda1f402852d4a93b334fab78db099ea', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3256, 'db-test-9477eccc48b2481082cafd96fe0c56d8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3314, 'db-test-1554052d33a7420680cf17de4d7b39fc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3315, 'db-test-690909145c22404788f2b71002c6db2a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3316, 'db-test-79382b0a0edd4b9bbd07ac17971b3df2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3317, 'db-test-28d8a7c16433473682e92e7d7f0d5a15', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3318, 'db-test-ee2409775f1c46ec948d2bdcebdb24a9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3319, 'db-test-d80bb898af2e4014819d4669f64029e2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3320, 'db-test-3fe338eaf2284db29543819b8bace803', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3321, 'db-test-7f05dc777140436191831f77f839c3dd', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3322, 'db-test-dc6c7e58503f416db185ce7c3d5baf3d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3323, 'db-test-d31ce7b6654c473c911b33d5022dae7e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3324, 'db-test-ecdc494040d840ad90f4431fb4939924', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3325, 'db-test-2f8b45f9e8f247d59a9b22512adf85d0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3326, 'db-test-cc82d628359c494b803426fec92bba1d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3327, 'db-test-0054837fef564e44acaef11c37ee26ef', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3328, 'db-test-b594d58508004e4d93e5e9ed6c19b09e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3329, 'db-test-8d8a975d8fbc4223a2f385186a9cc1f5', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3330, 'db-test-39dd6c32f549461886ed0feb5c228a3e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3331, 'db-test-63ef5fcb692046e2a1f1fe30fc42fa2b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3332, 'db-test-2b3f5fd955d94291809f9643a0d04da2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3390, 'db-test-749f1d44a2de4f83bc1305f747dc205c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3391, 'db-test-9df8d1c8519747ffaac244875186491a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3392, 'db-test-0f2b1c95db1c430589c419318ab644f7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3393, 'db-test-eb096881c8294c2089778184d72092bf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3394, 'db-test-9b14a339d9904844a817c519b1d78bc7', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3395, 'db-test-d2aefc1c39384b31aef2ea80b1437b67', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3396, 'db-test-d6678580a1794531b386ffe7815c0a63', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3397, 'db-test-d7d54946da274a1e9f933c04c68a414f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3398, 'db-test-a2ca78c15ba548d092aecb0c47ea5a34', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3399, 'db-test-7399e093dba740e7af6af702b96134e9', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3400, 'db-test-b091f0a33b8741f58a9279066f839930', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3401, 'db-test-34ba63253b294408a0e84ae4bdc2fd8e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3402, 'db-test-c3a70eeebbd04dd4b2436b665d66576b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3403, 'db-test-f92fe042af8c40459177417fa223e943', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3404, 'db-test-ebe469f1241b423894a7ac44e03e302f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3405, 'db-test-8f13ffe0ef9947318861e58f64039bed', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3406, 'db-test-d31bc0f5868d4b03bc83757bfedfd967', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3407, 'db-test-31c4bb6153864b53b00cb9523ada2726', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3408, 'db-test-f6ce612cea614566bac9e9f65aef9143', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3466, 'db-test-9a414c9907fc45feb1927f1a6f99d9ec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3467, 'db-test-30a63fb4e168488e85130525083e291d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3468, 'db-test-8b0986390ea14902b165e8ab6d38354a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3469, 'db-test-07c9d8de9a1b4907874dc34df867ac9a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3470, 'db-test-53472d4a92544c91a2ab4f30fb7f7d41', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3471, 'db-test-f5a2bc6646b348be81f9804f795490c8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3472, 'db-test-a5e3d60761f94bf385d97cd7c24d293c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3473, 'db-test-382374a675164e9da8aafe2cd337f9ec', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3474, 'db-test-72dcdcea4f91491c836b4d775b02b0f1', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3475, 'db-test-b6a76528324745eeaa0d1ab06d88116a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3476, 'db-test-65714402121a4c89b8fb2931ab661e58', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3477, 'db-test-61ae564413db4046962a4d6b6bc94dd6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3478, 'db-test-05799286a89e47da850d9c2f82be7c85', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3479, 'db-test-d54aebcfa8534cce802b53c87c404939', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3480, 'db-test-3188665c92724e838846522a727fcd5a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3481, 'db-test-36f0eccecb87439daa314f6db4ce50f2', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3482, 'db-test-2415507d34f1446d94780a60eb272c2c', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3483, 'db-test-f814e30b07a04131ae61353b749e9e7d', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3484, 'db-test-780c298dc55a44f58013e9660012be95', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3542, 'db-test-507c943bf24b41d5a910cbe2255d7a4a', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3543, 'db-test-8fd25bb250df4061956633c3c2cb1c25', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3544, 'db-test-02dba916926b483bbbfd045b88ec7809', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3545, 'db-test-495777bccb7740d7841eaec0011a5a8b', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3546, 'db-test-3a9484ba496e494e993d128c2275619f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3547, 'db-test-2969296f6ce646afad81a1b7183e6eb0', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3548, 'db-test-031d9f1a19bb4bd99f88b7c5c3ea89dc', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3549, 'db-test-0ff42348a67a45d6bcbd687958b6d49f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3550, 'db-test-ae2f0ded81d54cfa87192989249fd457', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3551, 'db-test-6bd9b6becda343d183d2ab1e0757ad91', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3552, 'db-test-012c794149fb41f3b7e2d264ac5643af', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3553, 'db-test-ca5b66dbc37b426d987ed8f9aa1330e8', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3554, 'db-test-7c66e253ec8341ae942af88ac13f46bf', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3555, 'db-test-ae6d402f9ec24102aadf7fa209ad6163', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3556, 'db-test-ccc91c7f0e5e45b192fea42a03e0f2c6', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3557, 'db-test-a9585777f8634f86b8b86a69e037df1f', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3558, 'db-test-2bc781bbab284dfcaa64addceaa80086', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3559, 'db-test-b905c058d4de4c1c95678dfbf4930b9e', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3560, 'db-test-726a4c8610814f55bc6dd208cb452585', 'v11-fixture-password-redacted');
INSERT INTO public.users OVERRIDING SYSTEM VALUE VALUES (3561, 'v11-migration-fixture', 'v11-fixture-password-redacted');
SELECT setval(pg_get_serial_sequence('public.users','id'), (SELECT max(id) FROM public.users));
--
-- PostgreSQL database dump
--

\restrict byNG1yv8qO3O5rk1RbXh5W0VyFMpX879lIARbk0hSDIbdif8MASVI0l08GhFS02

-- Dumped from database version 17.10 (Debian 17.10-1.pgdg13+1)
-- Dumped by pg_dump version 17.10 (Debian 17.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: figures; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1269, 1143, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1329, 1202, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1330, 1203, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1331, 1203, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1332, 1204, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1333, 1204, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1334, 1204, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1335, 1205, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1336, 1206, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1337, 1207, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1338, 1208, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1339, 1209, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1340, 1209, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (632, 574, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (633, 575, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (634, 575, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (635, 576, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (636, 576, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (637, 576, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (638, 577, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (639, 578, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (640, 579, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (641, 580, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (642, 581, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (643, 581, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (644, 581, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (645, 581, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (646, 582, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (780, 709, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (781, 710, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (782, 710, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (783, 711, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (784, 711, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (785, 711, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (786, 712, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (787, 713, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (788, 714, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (789, 715, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (790, 716, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (791, 716, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (792, 716, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (793, 716, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (794, 717, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1341, 1209, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1342, 1209, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1343, 1211, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1344, 1212, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1345, 1214, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1346, 1215, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1347, 1217, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1349, 1219, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (706, 641, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (707, 642, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (708, 642, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (709, 643, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (710, 643, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (711, 643, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (712, 644, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (713, 645, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (714, 646, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (715, 647, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (716, 648, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (717, 648, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (718, 648, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (719, 648, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (720, 649, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2320, 2135, 'rectangle', 273, 80, 532, 221);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1409, 1278, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1410, 1279, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1411, 1279, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1412, 1280, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1413, 1280, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1414, 1280, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1415, 1281, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1416, 1282, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1417, 1283, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (854, 776, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (855, 777, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (856, 777, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (857, 778, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (858, 778, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (859, 778, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (860, 779, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (861, 780, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (862, 781, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (863, 782, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (864, 783, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (865, 783, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (866, 783, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (867, 783, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (868, 784, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1418, 1284, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1419, 1285, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1420, 1285, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1421, 1285, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1422, 1285, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1423, 1287, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1424, 1288, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1425, 1290, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1426, 1291, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1427, 1293, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1429, 1295, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1489, 1354, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1490, 1355, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1491, 1355, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1492, 1356, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1493, 1356, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1494, 1356, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1495, 1357, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1496, 1358, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1497, 1359, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1498, 1360, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1499, 1361, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1500, 1361, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1501, 1361, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1502, 1361, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1503, 1363, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1504, 1364, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1025, 921, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1026, 922, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1027, 922, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1028, 923, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1029, 923, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1030, 923, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1031, 924, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1032, 925, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1033, 926, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1034, 927, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1035, 928, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (928, 843, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (929, 844, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (930, 844, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (931, 845, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (932, 845, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (933, 845, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (934, 846, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (935, 847, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (936, 848, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (937, 849, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (938, 850, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (939, 850, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (940, 850, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (941, 850, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (942, 851, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1036, 928, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1037, 928, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1038, 928, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1039, 929, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1099, 988, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1100, 989, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1101, 989, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1102, 990, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1103, 990, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1104, 990, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1105, 991, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1106, 992, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1107, 993, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1108, 994, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1109, 995, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1110, 995, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1111, 995, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1112, 995, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1113, 996, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1003, 911, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1004, 912, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1005, 912, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1006, 913, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1007, 913, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1008, 913, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1009, 914, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1010, 915, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1011, 916, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1012, 917, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1013, 918, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1014, 918, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1015, 918, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1016, 918, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1017, 919, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1173, 1055, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1174, 1056, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1175, 1056, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1176, 1057, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1177, 1057, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1178, 1057, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1179, 1058, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1180, 1059, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1181, 1060, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1182, 1061, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1183, 1062, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1184, 1062, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1185, 1062, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1186, 1062, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1187, 1063, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1247, 1122, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1248, 1123, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1249, 1123, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1250, 1124, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1251, 1124, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1252, 1124, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1253, 1125, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1254, 1126, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1255, 1127, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1256, 1128, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1257, 1129, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1258, 1129, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1259, 1129, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1260, 1129, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1261, 1131, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1262, 1132, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1263, 1134, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1264, 1135, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1265, 1137, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1267, 1139, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1268, 1141, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1505, 1366, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1506, 1367, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1507, 1369, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1509, 1371, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1569, 1430, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1570, 1431, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1571, 1431, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1572, 1432, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1573, 1432, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1574, 1432, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1575, 1433, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1576, 1434, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1577, 1435, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1578, 1436, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1579, 1437, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1580, 1437, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1581, 1437, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1582, 1437, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1583, 1439, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1584, 1440, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1585, 1442, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1586, 1443, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1587, 1445, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1589, 1447, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2315, 2133, 'rectangle', 401, 142, 556, 259);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2864, 2133, 'triangle', 732, 198, 848, 324);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2865, 2133, 'rectangle', 951, 113, 1059, 204);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2866, 2133, 'triangle', 892, 421, 979, 524);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1970, 1810, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1971, 1811, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1972, 1811, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1973, 1812, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1974, 1812, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1650, 1506, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1651, 1507, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1652, 1507, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1653, 1508, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1654, 1508, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1655, 1508, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1656, 1509, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1657, 1510, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1658, 1511, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1659, 1512, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1660, 1513, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1661, 1513, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1662, 1513, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1663, 1513, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1664, 1515, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1665, 1516, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1666, 1518, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1667, 1519, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1668, 1521, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1975, 1812, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1670, 1523, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1976, 1813, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1977, 1814, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1978, 1815, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1979, 1816, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1980, 1817, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1981, 1817, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1982, 1817, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1983, 1817, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1984, 1819, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1985, 1820, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1986, 1822, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1987, 1823, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1988, 1825, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2867, 2133, 'circle', 339, 472, 407, 540);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1990, 1827, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1730, 1582, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1731, 1583, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1732, 1583, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1733, 1584, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1734, 1584, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1735, 1584, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1736, 1585, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1737, 1586, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1738, 1587, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1739, 1588, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1740, 1589, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1741, 1589, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1742, 1589, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1743, 1589, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1744, 1591, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1745, 1592, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1746, 1594, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1747, 1595, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1748, 1597, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1750, 1599, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2940, 2706, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2941, 2707, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2942, 2707, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2943, 2708, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2944, 2708, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2945, 2708, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2946, 2709, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2947, 2710, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2948, 2711, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2949, 2712, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1810, 1658, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1811, 1659, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1812, 1659, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1813, 1660, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1814, 1660, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1815, 1660, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1816, 1661, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1817, 1662, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1818, 1663, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1819, 1664, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1820, 1665, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1821, 1665, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1822, 1665, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1823, 1665, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1824, 1667, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1825, 1668, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1826, 1670, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1827, 1671, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1828, 1673, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1830, 1675, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1890, 1734, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1891, 1735, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1892, 1735, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1893, 1736, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1894, 1736, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1895, 1736, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1896, 1737, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1897, 1738, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1898, 1739, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1899, 1740, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1900, 1741, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1901, 1741, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1902, 1741, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1903, 1741, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1904, 1743, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1905, 1744, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1906, 1746, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1907, 1747, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1908, 1749, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (1910, 1751, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2380, 2193, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2381, 2194, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2382, 2194, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2383, 2195, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2050, 1886, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2051, 1887, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2052, 1887, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2053, 1888, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2054, 1888, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2055, 1888, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2056, 1889, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2057, 1890, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2058, 1891, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2059, 1892, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2060, 1893, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2061, 1893, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2062, 1893, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2063, 1893, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2064, 1895, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2065, 1896, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2066, 1898, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2067, 1899, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2068, 1901, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2384, 2195, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2070, 1903, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2385, 2195, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2386, 2196, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2387, 2197, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2388, 2198, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2389, 2199, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2390, 2200, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2391, 2200, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2392, 2200, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2393, 2200, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2394, 2202, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2395, 2203, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2396, 2205, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2397, 2206, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2398, 2208, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2400, 2210, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2130, 1962, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2131, 1963, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2132, 1963, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2133, 1964, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2134, 1964, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2135, 1964, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2136, 1965, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2137, 1966, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2138, 1967, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2139, 1968, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2140, 1969, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2141, 1969, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2142, 1969, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2143, 1969, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2144, 1971, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2145, 1972, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2146, 1974, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2147, 1975, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2148, 1977, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2460, 2269, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2150, 1979, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2461, 2270, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2462, 2270, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2463, 2271, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2464, 2271, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2465, 2271, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2466, 2272, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2467, 2273, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2468, 2274, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2469, 2275, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2470, 2276, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2471, 2276, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2472, 2276, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2473, 2276, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2474, 2278, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2475, 2279, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2476, 2281, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2477, 2282, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2478, 2284, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2480, 2286, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2210, 2038, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2211, 2039, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2212, 2039, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2213, 2040, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2214, 2040, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2215, 2040, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2216, 2041, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2217, 2042, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2218, 2043, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2219, 2044, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2220, 2045, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2221, 2045, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2222, 2045, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2223, 2045, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2224, 2047, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2225, 2048, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2226, 2050, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2227, 2051, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2228, 2053, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2230, 2055, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2290, 2114, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2291, 2115, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2292, 2115, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2293, 2116, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2294, 2116, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2295, 2116, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2296, 2117, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2297, 2118, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2298, 2119, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2299, 2120, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2300, 2121, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2301, 2121, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2302, 2121, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2303, 2121, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2304, 2123, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2305, 2124, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2306, 2126, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2307, 2127, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2308, 2129, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2310, 2131, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2843, 2630, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2844, 2631, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2845, 2631, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2846, 2632, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2847, 2632, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2848, 2632, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2849, 2633, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2850, 2634, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2851, 2635, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2852, 2636, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2853, 2637, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2854, 2637, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2855, 2637, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2856, 2637, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2857, 2639, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2858, 2640, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2859, 2642, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2860, 2643, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2540, 2345, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2541, 2346, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2542, 2346, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2543, 2347, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2544, 2347, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2545, 2347, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2546, 2348, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2547, 2349, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2548, 2350, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2549, 2351, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2550, 2352, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2551, 2352, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2552, 2352, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2553, 2352, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2554, 2354, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2555, 2355, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2556, 2357, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2557, 2358, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2558, 2360, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2861, 2645, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2560, 2362, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2863, 2647, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3600, 3315, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3534, 516, 'circle', 663, 118, 831, 286);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3539, 516, 'rectangle', 753, 358, 935, 426);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3601, 3315, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3602, 3316, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3538, 516, 'triangle', 378, 209, 496, 288);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3537, 516, 'circle', 214, 389, 380, 555);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2620, 2421, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2621, 2422, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2622, 2422, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2623, 2423, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2624, 2423, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2625, 2423, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2626, 2424, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2627, 2425, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2628, 2426, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2629, 2427, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2630, 2428, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2631, 2428, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2632, 2428, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2633, 2428, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2634, 2430, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2635, 2431, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2636, 2433, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2637, 2434, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2638, 2436, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2640, 2438, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3599, 3314, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3603, 3316, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3604, 3316, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3605, 3317, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3606, 3318, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3607, 3319, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3608, 3320, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3609, 3321, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3610, 3321, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3611, 3321, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3612, 3321, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3613, 3323, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3614, 3324, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3615, 3326, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3616, 3327, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3617, 3329, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3619, 3331, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2700, 2497, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2701, 2498, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2702, 2498, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2703, 2499, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2704, 2499, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2705, 2499, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2706, 2500, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2707, 2501, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2708, 2502, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2709, 2503, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2710, 2504, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2711, 2504, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2712, 2504, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2713, 2504, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2714, 2506, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2715, 2507, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2716, 2509, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2717, 2510, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2718, 2512, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2720, 2514, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3679, 3390, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3680, 3391, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3681, 3391, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3682, 3392, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3683, 3392, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3684, 3392, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3685, 3393, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3686, 3394, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3687, 3395, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3688, 3396, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3689, 3397, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3690, 3397, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3691, 3397, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3692, 3397, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3693, 3399, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3694, 3400, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3695, 3402, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3696, 3403, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2950, 2713, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2951, 2713, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2952, 2713, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2953, 2713, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2954, 2715, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2955, 2716, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2956, 2718, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2957, 2719, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2958, 2721, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (2960, 2723, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3260, 3010, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3261, 3011, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3262, 3011, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3263, 3012, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3264, 3012, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3265, 3012, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3266, 3013, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3267, 3014, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3268, 3015, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3269, 3016, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3270, 3017, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3271, 3017, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3272, 3017, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3273, 3017, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3274, 3019, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3275, 3020, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3276, 3022, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3277, 3023, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3278, 3025, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3020, 2782, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3021, 2783, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3022, 2783, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3023, 2784, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3024, 2784, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3025, 2784, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3026, 2785, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3027, 2786, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3028, 2787, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3029, 2788, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3030, 2789, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3031, 2789, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3032, 2789, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3033, 2789, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3034, 2791, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3035, 2792, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3036, 2794, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3037, 2795, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3038, 2797, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3040, 2799, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3280, 3027, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3100, 2858, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3101, 2859, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3102, 2859, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3103, 2860, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3104, 2860, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3105, 2860, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3106, 2861, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3107, 2862, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3108, 2863, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3109, 2864, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3110, 2865, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3111, 2865, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3112, 2865, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3113, 2865, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3114, 2867, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3115, 2868, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3116, 2870, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3117, 2871, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3118, 2873, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3120, 2875, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3353, 3086, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3354, 3087, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3355, 3087, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3356, 3088, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3357, 3088, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3358, 3088, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3359, 3089, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3360, 3090, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3361, 3091, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3362, 3092, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3363, 3093, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3364, 3093, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3365, 3093, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3366, 3093, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3367, 3095, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3368, 3096, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3369, 3098, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3370, 3099, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3371, 3101, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3373, 3103, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3433, 3162, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3434, 3163, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3435, 3163, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3436, 3164, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3437, 3164, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3438, 3164, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3439, 3165, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3440, 3166, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3441, 3167, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3442, 3168, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3443, 3169, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3444, 3169, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3445, 3169, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3446, 3169, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3447, 3171, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3448, 3172, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3449, 3174, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3450, 3175, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3451, 3177, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3453, 3179, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3513, 3238, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3514, 3239, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3515, 3239, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3516, 3240, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3517, 3240, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3839, 3542, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3840, 3543, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3841, 3543, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3842, 3544, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3843, 3544, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3180, 2934, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3181, 2935, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3182, 2935, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3183, 2936, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3184, 2936, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3185, 2936, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3186, 2937, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3187, 2938, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3188, 2939, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3189, 2940, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3190, 2941, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3191, 2941, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3192, 2941, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3193, 2941, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3194, 2943, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3195, 2944, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3196, 2946, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3197, 2947, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3198, 2949, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3844, 3544, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3200, 2951, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3845, 3545, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3846, 3546, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3847, 3547, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3848, 3548, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3849, 3549, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3850, 3549, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3851, 3549, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3852, 3549, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3853, 3551, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3854, 3552, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3855, 3554, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3856, 3555, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3857, 3557, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3859, 3559, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3518, 3240, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3519, 3241, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3520, 3242, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3521, 3243, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3522, 3244, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3523, 3245, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3524, 3245, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3525, 3245, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3526, 3245, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3527, 3247, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3528, 3248, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3529, 3250, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3530, 3251, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3531, 3253, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3533, 3255, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3697, 3405, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3860, 3561, 'line', 100, 100, 300, 260);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3699, 3407, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3861, 3561, 'line', 400, 300, 600, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3862, 3561, 'line', 100, 400, 400, 400);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3863, 3561, 'line', 700, 100, 700, 400);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3864, 3561, 'rectangle', 200, 200, 500, 380);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3865, 3561, 'circle', 300, 250, 500, 450);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3866, 3561, 'triangle', 250, 300, 450, 500);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3867, 3561, 'rectangle', 280, 230, 520, 470);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3759, 3466, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3760, 3467, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3761, 3467, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3762, 3468, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3763, 3468, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3764, 3468, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3765, 3469, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3766, 3470, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3767, 3471, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3768, 3472, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3769, 3473, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3770, 3473, 'line', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3771, 3473, 'triangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3772, 3473, 'circle', 100, 100, 140, 140);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3773, 3475, 'rectangle', 20, 20, 30, 30);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3774, 3476, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3775, 3478, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3776, 3479, 'circle', 200, 200, 240, 240);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3777, 3481, 'rectangle', 0, 0, 10, 10);
INSERT INTO public.figures OVERRIDING SYSTEM VALUE VALUES (3779, 3483, 'line', 0, 0, 10, 10);


--
-- Name: figures_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.figures_id_seq', 3867, true);


--
-- PostgreSQL database dump complete
--

\unrestrict byNG1yv8qO3O5rk1RbXh5W0VyFMpX879lIARbk0hSDIbdif8MASVI0l08GhFS02

--
-- PostgreSQL database dump
--

\restrict ScoPaVH7TtcUulWebOjjOCJ7aonCPJDakgx4xlBHgkyenDId4ZjFBpizqToa4Ik

-- Dumped from database version 17.10 (Debian 17.10-1.pgdg13+1)
-- Dumped by pg_dump version 17.10 (Debian 17.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."__EFMigrationsHistory" VALUES ('20260714212457_InitialSchema', '10.0.10');


--
-- PostgreSQL database dump complete
--

\unrestrict ScoPaVH7TtcUulWebOjjOCJ7aonCPJDakgx4xlBHgkyenDId4ZjFBpizqToa4Ik

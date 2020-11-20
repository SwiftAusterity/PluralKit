-- SCHEMA VERSION 12: <insert date> --
-- Add disabling front/latch autoproxy per-member --
-- Add disabling autoproxy per-account --

alter table members add column allow_autoproxy bool not null default true;
alter table accounts add column allow_autoproxy bool not null default true;
update info set schema_version = 12;
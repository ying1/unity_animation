using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JSONHelper {

    [Serializable]
    public class SpaceTemplateCollection
    {
        public List<SpaceTemplate> space_templates = new List<SpaceTemplate>();
        public Pagination pagination;
    }

    [Serializable]
    public class KitCollection
    {
        public List<Kits> kits = new List<Kits>();
        public Pagination pagination;
    }

    [Serializable]
	public class SpaceTemplate
    {
        public string name;
        public string space_template_sid;
        public string space_manifest_url;
        public string selectable_by_non_admins;
        public string selectable_for_hangouts;
        public string activity_name;
        public string selectable_for_experiences;
        public string description;
        public string mobile_capacity;
        public string sdk;
        public string disabled_for_public_activities;
        public string admin;
        public string created_at;
        public string updated_at;
        public string tutorial;
        public string is_featured;
        public string space_template_id;
        public string recording_id;
        public List<AssetBundleScene> asset_bundle_scenes = new List<AssetBundleScene>();
        public string image_large;
        public string image_medium;
        public string image_small;
        public string image_thumbnail;
        public string banner_image_large;
        public string banner_image_medium;
        public string banner_image_small;
        
    }

    [Serializable]
    public class Kits
    {
        public string name;
        public string kit_type;
        public string is_featured;
        public string created_at;
        public string updated_at;
        public string id;
        public string kit_id;
        public string user_id;
        public List<AssetBundle> asset_bundles = new List<AssetBundle>();
        public string image_original;
        public string image_medium;
        public string image_thumbnail;
    }

    [Serializable]
    public class AssetBundleScene
    {
        public string aasm_state;
        public string name;
        public string created_at;
        public string updated_at;
        public string asset_bundle_scene_id;
        public string user_id;

        public List<AssetBundle> asset_bundles = new List<AssetBundle>();
    }

    [Serializable]
    public class AssetBundle
    {
        public string game_engine;
        public string game_engine_version;
        public string platform;
        public string url;
        public string description;
        public string aasm_state;
        public string created_at;
        public string update_at;
        public string layout;
        public string crc;
        public string client_cache_key;
        public string name;
        public string asset_bundle_id;
        public string user_id;
        public string asset_bundle_scene_id;
        public string avatar_id;
    }

    [Serializable]
    public class Pagination
    {
        public int page;
        public int pages;
        public int count;
    }
}

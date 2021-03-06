﻿namespace TrafficManager.U {
    using System;
    using System.Collections.Generic;
    using ColossalFramework.UI;
    using TrafficManager.UI.Textures;
    using UnityEngine;

    public static class TextureUtil {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteAtlas"/> class from resource names list.
        /// </summary>
        /// <param name="atlasName">Name for the new atlas.</param>
        /// <param name="resourcePrefix">Prefix to resource directory.</param>
        /// <param name="spriteNames">Array of paths to load also they become atlas keys.</param>
        /// <param name="spriteWidth">Hint width for texture loader (todo: get rid of this).</param>
        /// <param name="spriteHeight">Hint height for texture loader (todo: get rid of this).</param>
        /// <param name="hintAtlasTextureSize">Square texture with this side size is created.</param>
        public static UITextureAtlas CreateAtlas(string atlasName,
                                                 string resourcePrefix,
                                                 string[] spriteNames,
                                                 int spriteWidth,
                                                 int spriteHeight,
                                                 int hintAtlasTextureSize) {
            Texture2D texture2D = new Texture2D(
                hintAtlasTextureSize,
                hintAtlasTextureSize,
                TextureFormat.ARGB32,
                false);
            var textures = new List<Texture2D>(spriteNames.Length);

            // Load separate sprites and then pack it in a texture together
            foreach (string spriteName in spriteNames) {
                Texture2D loadedSprite = TextureResources.LoadDllResource(
                    $"{resourcePrefix}.{spriteName}.png",
                    spriteWidth,
                    spriteHeight);
                textures.Add(loadedSprite);
            }

            var regions = texture2D.PackTextures(textures.ToArray(), 2, hintAtlasTextureSize);

            // Now using loaded and packed textures, create the atlas with sprites
            UITextureAtlas newAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            var uiView = UIView.GetAView();
            Material material = UnityEngine.Object.Instantiate(uiView.defaultAtlas.material);

            material.mainTexture = texture2D;
            newAtlas.material = material;
            newAtlas.name = atlasName;

            for (int i = 0; i < spriteNames.Length; i++) {
                var item = new UITextureAtlas.SpriteInfo {
                                                             name = spriteNames[i],
                                                             texture = textures[i],
                                                             region = regions[i],
                                                         };

                newAtlas.AddSprite(item);
            }

            return newAtlas;
        }

        // [Obsolete("Replace with ModUI global atlas and Unity atlas function call")]
        // public static UITextureAtlas GenerateLinearAtlas(string name,
        //                                                  Texture2D texture,
        //                                                  int numSprites,
        //                                                  string[] spriteNames) {
        //     return Generate2DAtlas(name, texture, numSprites, 1, spriteNames);
        // }

        [Obsolete("Remove and use U.ButtonTexture instead")]
        public static UITextureAtlas Generate2DAtlas(string name,
                                                     Texture2D texture,
                                                     int numX,
                                                     int numY,
                                                     string[] spriteNames) {
            if (spriteNames.Length != numX * numY) {
                throw new ArgumentException(
                    "Number of sprite name does not match dimensions " +
                    $"(expected {numX} x {numY}, was {spriteNames.Length})");
            }

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.padding = 0;
            atlas.name = name;

            Shader shader = Shader.Find("UI/Default UI Shader");

            if (shader != null) {
                atlas.material = new Material(shader);
            }

            atlas.material.mainTexture = texture;

            int spriteWidth = Mathf.RoundToInt(texture.width / (float)numX);
            int spriteHeight = Mathf.RoundToInt(texture.height / (float)numY);
            int k = 0;

            for (int i = 0; i < numX; ++i) {
                float x = i / (float)numX;
                for (int j = 0; j < numY; ++j) {
                    float y = j / (float)numY;

                    var sprite = new UITextureAtlas.SpriteInfo {
                        name = spriteNames[k],
                        region = new Rect(
                            x,
                            y,
                            spriteWidth / (float)texture.width,
                            spriteHeight / (float)texture.height),
                    };

                    var spriteTexture = new Texture2D(spriteWidth, spriteHeight);
                    spriteTexture.SetPixels(
                        texture.GetPixels(
                            (int)(texture.width * sprite.region.x),
                            (int)(texture.height * sprite.region.y),
                            spriteWidth,
                            spriteHeight));
                    sprite.texture = spriteTexture;

                    atlas.AddSprite(sprite);

                    ++k;
                }
            }

            return atlas;
        }
    } // end class
}
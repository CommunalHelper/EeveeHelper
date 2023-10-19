local holdableContainer = {
    name = "EeveeHelper/HoldableContainer",
    fillColor = { 1.0, 0.6, 0.6, 0.4 },
    borderColor = { 1.0, 0.6, 0.6, 1 },

    placements = {
        default = {
            data = {
                width = 8,
                height = 8,
                whitelist = "",
                blacklist = "",
                containMode = "RoomStart",
                containFlag = "",
                fitContained = true,
                ignoreAnchors = false,
                forceStandardBehavior = false,
                ignoreContainerBounds = false,
                gravity = true,
                holdable = true,
                noDuplicate = false,
                slowFall = false,
                slowRun = true,
                destroyable = true,
                tutorial = false,
                respawn = false,
                waitForGrab = false,
            }
        },
        {
            name = "holdable",
            data = {
                width = 8,
                height = 8,
            }
        },
        {
            name = "falling",
            data = {
                width = 8,
                height = 8,
                holdable = false,
            }
        }
    },
}

local attachedContainer = {
    name = "EeveeHelper/AttachedContainer",
    fillColor = { 1.0, 0.6, 0.6, 0.4 },
    borderColor = { 1.0, 0.6, 0.6, 1 },
    nodeLimits = { 0, 1 },
    nodeLineRenderType = "line",

    placements = {
        name = "default",
        data = {
            width = 8,
            height = 8,
            whitelist = "",
            blacklist = "",
            containMode = "RoomStart",
            containFlag = "",
            fitContained = true,
            ignoreAnchors = false,
            forceStandardBehavior = false,
            ignoreContainerBounds = false,
            attachMode = "RoomStart",
            attachFlag = "",
            attachTo = "",
            restrictToNode = true,
            onlyX = false,
            onlyY = false,
            matchCollidable = false,
            matchVisible = false,
            destroyable = true,
        }
    },
    fieldOrder = { "x", "y", "width", "height", "containMode", "containFlag", "whitelist", "blacklist", "attachMode", "attachFlag", "attachTo" },
}

local floatyContainer = {
    name = "EeveeHelper/FloatyContainer",
    fillColor = { 1.0, 0.6, 0.6, 0.4 },
    borderColor = { 1.0, 0.6, 0.6, 1 },

    placements = {
        name = "default",
        data = {
            width = 8,
            height = 8,
            whitelist = "",
            blacklist = "",
            containMode = "RoomStart",
            containFlag = "",
            ignoreAnchors = false,
            forceStandardBehavior = false,
            ignoreContainerBounds = false,
            floatSpeed = 1.0,
            floatMove = 4.0,
            pushSpeed = 1.0,
            pushMove = 8.0,
            sinkSpeed = 1.0,
            sinkMove = 12.0,
            disableSpawnOffset = false,
            disablePush = false,
        }
    },
    fieldOrder = { "x", "y", "width", "height", "containMode", "containFlag", "whitelist", "blacklist", "floatMove", "floatSpeed", "pushMove", "pushSpeed", "sinkMove", "sinkSpeed" },
}

local SMWTrackContainer = {
    name = "EeveeHelper/SMWTrackContainer",
    fillColor = { 1.0, 0.6, 0.6, 0.4 },
    borderColor = { 1.0, 0.6, 0.6, 1 },

    placements = {
        default = {
            data = {
                width = 8,
                height = 8,
                whitelist = "",
                blacklist = "",
                containMode = "RoomStart",
                containFlag = "",
                fitContained = true,
                ignoreAnchors = false,
                forceStandardBehavior = false,
                ignoreContainerBounds = false,
                moveSpeed = 100.0,
                fallSpeed = 200.0,
                gravity = 200.0,
                startDelay = 0.0,
                direction = "Right",
                moveFlag = "",
                moveBehaviour = "Linear",
                easing = "SineInOut",
                easeDuration = 2.0,
                easeTrackDirection = false,
                startOnTouch = false,
                stopAtNode = false,
                stopAtEnd = false,
                moveOnce = false,
                disableBoost = false,
            }
        },
        {
            name = "linear",
            data = {
                width = 40,
                height = 8,
                moveBehaviour = "Linear"
            }
        },
        {
            name = "easing",
            data = {
                width = 40,
                height = 8,
                moveBehaviour = "Easing"
            }
        }
    },

    fieldInformation = {
        easing = {
            options = { "Linear", "SineIn", "SineOut", "SineInOut", "QuadIn", "QuadOut", "QuadInOut", "CubeIn", "CubeOut", "CubeInOut", "QuintIn", "QuintOut", "QuintInOut", "ExpoIn", "ExpoOut", "ExpoInOut" },
            editable = false
        },
        moveBehaviour = {
            options = { "Linear", "Easing" },
            editable = false,
        }
    },

    ignoredFields = function (entity)
        local ignored = {"_name", "_id", "originX", "originY"}

        if entity.moveBehaviour == "Linear" then
            table.insert(ignored, "easing")
            table.insert(ignored, "easeDuration")
            table.insert(ignored, "easeTrackDirection")

        elseif entity.moveBehaviour == "Easing" then
            table.insert(ignored, "moveSpeed")

        end

        return ignored
    end,
}

local flagGateContainer = {
    name = "EeveeHelper/FlagGateContainer",
    fillColor = { 1.0, 0.6, 0.6, 0.4 },
    borderColor = { 1.0, 0.6, 0.6, 1 },
    nodeLimits = {2, 2},
    nodeLineRenderType = "line",

    placements = {
        default = {
            data = {
                width = 8,
                height = 8,
                whitelist = "",
                blacklist = "",
                containMode = "RoomStart",
                containFlag = "",
                fitContained = true,
                ignoreAnchors = false,
                forceStandardBehavior = false,
                ignoreContainerBounds = false,
                moveFlag = "",
                shakeTime = 0.5,
                moveTime = 2.0,
                easing = "CubeOut",
                icon = "objects/switchgate/icon",
                inactiveColor = "5FCDE4",
                activeColor = "FFFFFF",
                finishColor = "F141DF",
                staticFit = false,
                canReturn = false,
                iconVisible = true,
                playSounds = true,
            }
        },
        {
            name = "switchGate",
            data = {
                width = 8,
                height = 8,
            }
        },
        {
            name = "flagMover",
            data = {
                width = 8,
                height = 8,
                shakeTime = 0.0,
                moveFlag = "flag",
                canReturn = true,
                iconVisible = false,
                playSounds = false
            }
        }
    }
}

local flagToggleModifier = {
    name = "EeveeHelper/FlagToggleModifier",
    fillColor = { 0.6, 1.0, 0.6, 0.4 },
    borderColor = { 0.6, 1.0, 0.6, 1 },

    placements = {
        name = "default",
        data = {
            width = 8,
            height = 8,
            whitelist = "",
            blacklist = "",
            containMode = "RoomStart",
            containFlag = "",
            forceStandardBehavior = false,
            ignoreContainerBounds = false,
            flag = "",
            notFlag = false,
            toggleActive = true,
            toggleVisible = true,
            toggleCollidable = true,
            rememberInitialState = true,
            delayedToggle = false,
        }
    }
}

local collidableModifier = {
    name = "EeveeHelper/CollidableModifier",
    fillColor = { 0.6, 1.0, 0.6, 0.4 },
    borderColor = { 0.6, 1.0, 0.6, 1 },

    placements = {
        default = {
            data = {
                width = 8,
                height = 8,
                whitelist = "",
                blacklist = "",
                containMode = "RoomStart",
                containFlag = "",
                forceStandardBehavior = false,
                ignoreContainerBounds = false,
                collisionMode = "NoCollide",
                keepBaseCollision = false
            }
        },
        {
            name = "noCollide",
            data = {
                width = 8,
                height = 8,
                collisionMode = "NoCollide"
            }
        },
        {
            name = "solidify",
            data = {
                width = 8,
                height = 8,
                collisionMode = "Solid"
            }
        },
        {
            name = "hazardous",
            data = {
                width = 8,
                height = 8,
                collisionMode = "Hazardous"
            }
        }
    },

    fieldInformation = {
        collisionMode = {
            options = { "NoCollide", "Solid", "Hazardous" },
            editable = false,
        }
    }
}

local depthModifier = {
    name = "EeveeHelper/DepthModifier",
    fillColor = { 0.6, 1.0, 0.6, 0.4 },
    borderColor = { 0.6, 1.0, 0.6, 1 },

    placements = {
        name = "default",
        data = {
            width = 8,
            height = 8,
            whitelist = "",
            blacklist = "",
            containMode = "RoomStart",
            containFlag = "",
            forceStandardBehavior = false,
            ignoreContainerBounds = false,
            depth = -9000,
        }
    }
}

local globalModifier = {
    name = "EeveeHelper/GlobalModifier",
    fillColor = { 0.6, 1.0, 0.6, 0.4 },
    borderColor = { 0.6, 1.0, 0.6, 1 },

    placements = {
        name = "default",
        data = {
            width = 8,
            height = 8,
            whitelist = "",
            frozenUpdate = false,
            pauseUpdate = false,
            transitionUpdate = false,
        }
    }
}

local debugContainer = {
    name = "EeveeHelper/DebugContainer",
    fillColor = { 0.6, 0.6, 1.0, 0.4 },
    borderColor = { 0.6, 0.6, 1.0, 1 },

    placements = {
        name = "default",
        data = {
            width = 8,
            height = 8,
            containMode = "RoomStart",
        }
    }
}

local containers = {
    holdableContainer,
    attachedContainer,
    floatyContainer,
    SMWTrackContainer,
    flagGateContainer,
    flagToggleModifier,
    collidableModifier,
    depthModifier,
    globalModifier,
    debugContainer
}

local containModes = { "RoomStart", "FlagChanged", "Always", "DelayedRoomStart" }
local directions = { "Left", "Right" }
local easeTypes = { "Linear", "SineIn", "SineOut", "SineInOut", "QuadIn", "QuadOut", "QuadInOut", "CubeIn", "CubeOut", "CubeInOut", "QuintIn", "QuintOut", "QuintInOut", "BackIn", "BackOut", "BackInOut", "ExpoIn", "ExpoOut", "ExpoInOut", "BigBackIn", "BigBackOut", "BigBackInOut", "ElasticIn", "ElasticOut", "ElasticInOut", "BounceIn", "BounceOut", "BounceInOut" }

-- stolen from crystalline helper (with permission of course)
-- see: https://github.com/CommunalHelper/CrystallineHelper/blob/dev/Loenn/triggers/edit_depth_trigger.lua
local depths = {
    {"BG Terrain (10000)", 10000},
    {"BG Mirrors (9500)", 9500},
    {"BG Decals (9000)", 9000},
    {"BG Particles (8000)", 8000},
    {"Solids Below (5000)", 5000},
    {"Below (2000)", 2000},
    {"NPCs (1000)", 1000},
    {"Theo Crystal (100)", 100},
    {"Player (0)", 0},
    {"Dust (-50)", -50},
    {"Pickups (-100)", -100},
    {"Seeker (-200)", -200},
    {"Particles (-8000)", -8000},
    {"Above (-8500)", -8500},
    {"Solids (-9000)", -9000},
    {"FG Terrain (-10000)", -10000},
    {"FG Decals (-10500)", -10500},
    {"Dream Blocks (-11000)", -11000},
    {"Crystal Spinners (-11500)", -11500},
    {"Player Dreamdashing (-12000)", -12000},
    {"Enemy (-12500)", -12500},
    {"Fake Walls (-13000)", -13000},
    {"FG Particles (-50000)", -50000},
    {"Top (-1000000)", -1000000},
    {"Formation Sequences (-2000000)", -2000000},
}


local sharedFieldInformation = {
    containMode = {
        options = containModes,
        editable = false
    },
    attachMode = {
        options = containModes,
        editable = false
    },
    easing = {
        options = easeTypes,
        editable = false
    },
    direction = {
        options = directions,
        editable = false
    },
    depth = {
        fieldType = "integer",
        options = depths
    },
    inactiveColor = {
        fieldType = "color"
    },
    activeColor = {
        fieldType = "color"
    },
    finishColor = {
        fieldType = "color"
    },
}

for _, container in ipairs(containers) do
    container.fieldInformation = container.fieldInformation or {}
    for k, v in pairs(sharedFieldInformation) do
        -- only add shared field information if it doesn't already exist
        if container.fieldInformation[k] == nil then
            container.fieldInformation[k] = v
        end
    end
    container.depth = math.huge -- make containers render below everything
end

return containers
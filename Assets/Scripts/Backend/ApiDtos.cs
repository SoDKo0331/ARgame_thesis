using System;
using System.Collections.Generic;

[Serializable]
public class GuestLoginRequestDto
{
    public string deviceId;
    public string displayName;
}

[Serializable]
public class HealthCheckResponseDto
{
    public string status;
    public string service;
}

[Serializable]
public class UserDto
{
    public string id;
    public string deviceId;
    public string displayName;
    public string lastLoginAt;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class GuestLoginResponseDto
{
    public UserDto user;
    public string accessToken;
    public bool isNewUser;
}

[Serializable]
public class RewardDto
{
    public string id;
    public string name;
    public string description;
    public string imageUrl;
    public string previewPrefabKey;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class SpotDto
{
    public string id;
    public string name;
    public string description;
    public double latitude;
    public double longitude;
    public float distanceMeters;
    public float radiusMeters;
    public bool isActive;
    public string modelPrefabKey;
    public string createdAt;
    public string updatedAt;
    public RewardDto reward;
}

[Serializable]
public class SpotsResponseDto
{
    public List<SpotDto> spots;
}

[Serializable]
public class SpotDetailsResponseDto
{
    public SpotDto spot;
}

[Serializable]
public class ClaimRewardRequestDto
{
    public string userId;
    public bool hasLocation;
    public double latitude;
    public double longitude;
    public float horizontalAccuracyMeters;
}

[Serializable]
public class ClaimDto
{
    public string id;
    public string userId;
    public string claimedAt;
    public RewardDto reward;
    public SpotDto tourismSpot;
}

[Serializable]
public class ClaimRewardResponseDto
{
    public bool alreadyClaimed;
    public RewardDto reward;
    public SpotDto tourismSpot;
    public ClaimDto claim;
}

[Serializable]
public class UserRewardsResponseDto
{
    public string userId;
    public List<ClaimDto> rewards;
}

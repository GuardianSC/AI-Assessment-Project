#pragma once
/*
    Simple and robust Vector2 math library
*/
#include <cmath>


union Vector2
{
    struct { float x, y; };
    float v[2];

    static Vector2 fromXZ(float *vec3)  { return Vector2{ vec3[0],vec3[2] }; }
    static constexpr Vector2 zero()     { return{0,0}; }
    static Vector2 fromAngle(float a)   { return {cos(a),sin(a)}; }

    static Vector2 random();

    float &operator[](unsigned idx)      { return v[idx]; }
    float operator[](unsigned idx) const { return v[idx]; }
};

inline Vector2 operator+(const Vector2 &a, const Vector2 &b) { return Vector2{ a.x + b.x, a.y + b.y }; }
inline Vector2 operator-(const Vector2 &a, const Vector2 &b) { return Vector2{ a.x - b.x, a.y - b.y }; }

inline Vector2 operator*(const Vector2 &a, const Vector2 &b) { return Vector2{ a.x * b.x, a.y * b.y }; }
inline Vector2 operator*(const Vector2 &a, float b)          { return Vector2{ a.x * b  , a.y * b   }; }
inline Vector2 operator*(float b, const Vector2 &a)          { return Vector2{ a.x * b  , a.y * b   }; }
inline Vector2 operator/(const Vector2 &a, float b)          { return Vector2{ a.x / b  , a.y / b   }; }

inline Vector2 &operator+=(Vector2 &a, const Vector2 &b) { return a = a + b; }
inline Vector2 &operator-=(Vector2 &a, const Vector2 &b) { return a = a - b; }
inline Vector2 &operator*=(Vector2 &a, float b)          { return a = a * b; }
inline Vector2 &operator/=(Vector2 &a, float b)          { return a = a / b; }

// Scalar Projection, also called the dot product, also called 'cosine angle between'
inline float   dot(const Vector2 &a, const Vector2 &b) { return a.x*b.x + a.y*b.y; }

// determine vector's length
inline float magnitude  (const Vector2 &a)            { return sqrtf(dot(a, a));  }
inline Vector2 operator-(const Vector2 &a)            { return a * -1;            }

// vector of length 1
inline Vector2 normal   (const Vector2 &a)            { return a / magnitude(a);  }

Vector2 Vector2::random()
{
    return normal(Vector2{ 1 - 2 * (float)rand() / RAND_MAX, 1 - 2 * (float)rand() / RAND_MAX });
}

inline float distance(const Vector2 &a, const Vector2 &b) { return magnitude(a - b); }

//component-wise min and max
inline Vector2 min(const Vector2 &a, const Vector2 &b) { return Vector2{ fminf(a.x,b.x),fminf(a.y,b.y) }; }
inline Vector2 max(const Vector2 &a, const Vector2 &b) { return Vector2{ fmaxf(a.x,b.x),fmaxf(a.y,b.y) }; }

// component-wise clamp
inline Vector2 clamp(const Vector2 &a_val, const Vector2 &a_min, const Vector2 &a_max) { return max(a_min,min(a_val,a_max)); }

// if a and b make a box, snap calculates the closest point on the box to q.
inline Vector2 snap(const Vector2 &q, const Vector2 &a, const Vector2 &b)              { return Vector2{ fabsf(q.x - a.x) < fabsf(q.x - b.x) ? a.x : b.x, fabsf(q.y - a.y) < fabsf(q.y - b.y) ? a.y : b.y }; }

// linear interpolation between start and end based on the alpha provided
inline Vector2 lerp(const Vector2 &start, const Vector2 &end, float a) { return (1 - a)*start + a*end; }

// clamp the vector's length between 1 and the provided maximum
inline Vector2 truncate(const Vector2 &a, float max) { return clamp(a, normal(a), normal(a)*max); }

inline Vector2 perp(const Vector2 &a, bool right = false) { return Vector2{ (right*2 - 1) * a.y, (1 - 2*right) * a.x}; }

inline Vector2 project(const Vector2 &incident, const Vector2 &normal) { return dot(incident, normal)*normal; }
inline Vector2 reflect(const Vector2 &incident, const Vector2 &normal) { return incident - 2*dot(incident,normal)*normal; }

// calculate the angle between two vectors
inline float angle(const Vector2 &a, const Vector2 &b) { return dot(a, b) / (magnitude(a)*magnitude(b)); }

// calculate the angle of a 2D vector
float angle(const Vector2 &a) { return atan2f(a.y, a.x); }

// there is no 2D cross product, this is actually the perpendicular dot product.
// negative values mean b is to the "left" of a and positive values mean b is to the "right" of a.
inline float cross(const Vector2 &a, const Vector2 &b) { return dot(a, perp(b)); }


#ifndef FLOAT_FUDGE
#define NEED_FUDGE_UNDEF
#define FLOAT_FUDGE 0.0001f
#endif

inline bool operator<(const Vector2 &a, const Vector2 &b) { return a.x < b.x - FLOAT_FUDGE && 
                                                                   a.y < b.y - FLOAT_FUDGE; }

inline bool operator==(const Vector2 &a, const Vector2 &b) { return !(a < b || b < a); }
inline bool operator<=(const Vector2 &a, const Vector2 &b) { return   a < b || a == b; }
inline bool operator>(const Vector2 &a, const Vector2 &b)  { return !(a <= b); }
inline bool operator>=(const Vector2 &a, const Vector2 &b) { return !(a <  b); }

inline bool vequals(const Vector2 &a, const Vector2 &b, float fudge = FLOAT_FUDGE) 
{
    return distance(a, b) < fudge;
}


#ifdef NEED_FUDGE_UNDEF
#undef NEED_FUDGE_UNDEF
#undef FLOAT_FUDGE
#endif
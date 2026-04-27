# --- Cat uploads bucket (private, SSE-S3, CORS for presigned browser access) ---

resource "aws_s3_bucket" "cat_uploads" {
  bucket = "${local.name_prefix}-cats-${random_id.suffix.hex}"
}

resource "aws_s3_bucket_public_access_block" "cat_uploads" {
  bucket = aws_s3_bucket.cat_uploads.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "cat_uploads" {
  bucket = aws_s3_bucket.cat_uploads.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "cat_uploads" {
  bucket = aws_s3_bucket.cat_uploads.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_cors_configuration" "cat_uploads" {
  bucket = aws_s3_bucket.cat_uploads.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "HEAD"]
    allowed_origins = distinct(concat(
      ["https://${aws_cloudfront_distribution.web.domain_name}"],
      split(",", replace(var.cat_bucket_cors_origins, " ", ""))
    ))
    expose_headers  = ["ETag"]
    max_age_seconds = 3600
  }
}

# --- Web static bucket + CloudFront OAC ---

resource "aws_s3_bucket" "web" {
  bucket = "${local.name_prefix}-web-${random_id.suffix.hex}"
}

resource "aws_s3_bucket_public_access_block" "web" {
  bucket = aws_s3_bucket.web.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "web" {
  bucket = aws_s3_bucket.web.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_policy" "web" {
  bucket = aws_s3_bucket.web.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowCloudFrontRead"
        Effect = "Allow"
        Principal = {
          Service = "cloudfront.amazonaws.com"
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.web.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.web.arn
          }
        }
      }
    ]
  })
}

resource "aws_cloudfront_origin_access_control" "web" {
  name                              = "${local.name_prefix}-oac"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

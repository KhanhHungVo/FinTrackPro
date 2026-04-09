terraform {
  required_version = ">= 1.7"

  required_providers {
    render = {
      source  = "render-oss/render"
      version = "~> 1.8"
    }
  }

  cloud {
    # Update organization to match your Terraform Cloud org name.
    # Run `terraform login` once before `terraform init`.
    organization = "fintrackpro"

    workspaces {
      name = "fintrackpro-prod"
    }
  }
}

provider "render" {
  api_key  = var.render_api_key
  owner_id = var.render_owner_id
}

"use client";

import { useParams } from "next/navigation";
import { AdvertisingDetailPanel } from "@/components/advertising-workspace";

export default function AdSetDetailPage() {
  const params = useParams<{ adSetId: string }>();
  return <AdvertisingDetailPanel kind="ad-set" id={params.adSetId} />;
}
